using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LgTvControl.LgConnect.Packets;
using LgTvControl.LgConnect.Packets.ClientBound;
using LgTvControl.LgConnect.Packets.ServerBound;
using MoonCore.Helpers;

namespace LgTvControl.LgConnect;

public class LgConnectClient
{
    public bool IsConnected => WebSocket != null && WebSocket.State == WebSocketState.Open;

    public int Channel { get; private set; }
    public int Volume { get; private set; }
    public bool ScreenOn { get; private set; }
    public LgConnectInput Input { get; private set; } = LgConnectInput.Unknown;
    public LgConnectState State { get; private set; } = LgConnectState.Offline;
    public bool Mute { get; private set; }

    public event Func<string, Task>? OnMessageReceived;
    public event Func<LgConnectState, Task>? OnStateChanged;
    public event Func<Exception, Task>? OnWebsocketError;
    public event Func<Exception, Task>? OnMessageError;
    public event Func<Exception, Task>? OnSubscriptionError;
    public event Func<string, Task>? OnClientKeyChanged;
    public event Func<bool, Task>? OnScreenStateChanged;
    public event Func<int, Task>? OnChannelChanged;
    public event Func<int, Task>? OnVolumeChanged;
    public event Func<bool, Task>? OnMuteChanged;
    public event Func<LgConnectInput, Task>? OnInputChanged;

    private readonly string Host;
    private readonly int Port;

    private readonly List<LgConnectSubscription> Subscriptions = new();

    private const string Prefix = "5d3ed79";

    private ClientWebSocket? WebSocket;
    private int PacketCounter = 1;
    private CancellationTokenSource Cancellation;

    public LgConnectClient(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public Task Start()
    {
        Cancellation = new();

        Task.Run(Loop);

        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        await Cancellation.CancelAsync();
    }

    private async Task Loop()
    {
        while (!Cancellation.IsCancellationRequested)
        {
            try
            {
                await UpdateState(LgConnectState.Offline);

                // Reset state
                PacketCounter = 1;

                // Create web socket
                WebSocket = new();
                WebSocket.Options.SetBuffer(1016, 1016);
                WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);

                try
                {
                    var cts = new CancellationTokenSource(
                        TimeSpan.FromSeconds(5)
                    );

                    await WebSocket.ConnectAsync(
                        new Uri($"ws://{Host}:{Port}"),
                        cts.Token
                    );
                }
                catch (OperationCanceledException)
                {
                    // Reconnect
                    continue;
                }
                catch (Exception e)
                {
                    if (OnWebsocketError != null)
                        await OnWebsocketError.Invoke(e);

                    // Throttle a bit before reconnecting
                    await Task.Delay(
                        TimeSpan.FromSeconds(3)
                    );

                    continue;
                }

                await UpdateState(LgConnectState.Connected);

                while (WebSocket.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024];

                    try
                    {
                        var receiveResult = await WebSocket.ReceiveAsync(buffer, Cancellation.Token);
                        var resizedBuffer = new byte[receiveResult.Count];
                        Array.Copy(buffer, resizedBuffer, resizedBuffer.Length);

                        var text = Encoding.UTF8.GetString(resizedBuffer);

                        if (OnMessageReceived != null)
                            await OnMessageReceived.Invoke(text);

                        if (text.Contains("403 too many pairing requests", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Close connection
                            if (WebSocket.State == WebSocketState.Open)
                            {
                                await WebSocket.CloseOutputAsync(
                                    WebSocketCloseStatus.Empty,
                                    null,
                                    Cancellation.Token
                                );
                            }

                            // We are sending too many pairing requests, lets wait a bit
                            await Task.Delay(
                                TimeSpan.FromSeconds(3),
                                Cancellation.Token
                            );
                        }
                        else if (text.Contains("403 Error!! power state"))
                        {
                            // Close connection
                            if (WebSocket.State == WebSocketState.Open)
                            {
                                await WebSocket.CloseOutputAsync(
                                    WebSocketCloseStatus.Empty,
                                    null,
                                    Cancellation.Token
                                );
                            }

                            // TV is reporting an invalid power state. Closing connection and waiting a bit
                            await Task.Delay(
                                TimeSpan.FromSeconds(1),
                                Cancellation.Token
                            );
                        }
                        else if (text.Contains("pairingType\":\"PROMPT"))
                        {
                            await UpdateState(LgConnectState.Pairing);
                        }
                        else
                        {
                            Task.Run(() => HandleRawMessage(text));
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignored
                    }
                    catch (Exception e)
                    {
                        if (OnWebsocketError != null)
                            await OnWebsocketError.Invoke(e);
                    }
                }
            }
            catch (Exception e)
            {
                if (OnWebsocketError != null)
                    await OnWebsocketError.Invoke(e);
            }
        }
    }

    private async Task HandleRawMessage(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(message))
                return;

            var basePacket = JsonSerializer.Deserialize<BasePacket>(message);

            if (basePacket == null)
                return;

            if (basePacket.Type == "registered")
            {
                // Reset subscriptions
                lock (Subscriptions)
                    Subscriptions.Clear();

                await UpdateState(LgConnectState.Ready);

                await SendInitialSubscriptions();

                var pairingResponse = JsonSerializer.Deserialize<BasePacket<PairingResponse>>(
                    message
                );

                if (pairingResponse == null)
                    return;

                if (OnClientKeyChanged != null)
                    await OnClientKeyChanged.Invoke(pairingResponse.Payload.ClientKey);
            }
            else
            {
                // Everything else should be a subscription
                LgConnectSubscription? subscription;

                lock (Subscriptions)
                {
                    subscription = Subscriptions
                        .FirstOrDefault(x => x.PacketId == basePacket.Id);
                }

                if (subscription == null)
                    return;

                var payloadPacket = JsonSerializer.Deserialize(
                    message,
                    typeof(BasePacket<>).MakeGenericType(subscription.PayloadType)
                );

                if (payloadPacket == null)
                    return;

                var payload = payloadPacket
                    .GetType()
                    .GetProperty("Payload")!
                    .GetValue(payloadPacket);

                var funcType = typeof(Func<,>).MakeGenericType(subscription.PayloadType, typeof(Task));
                var funcInvokeMethod = funcType.GetMethod("Invoke")!;

                foreach (var callback in subscription.Callbacks)
                {
                    try
                    {
                        var invokeTask = funcInvokeMethod.Invoke(callback, [payload]) as Task;
                        await invokeTask!;
                    }
                    catch (Exception e)
                    {
                        if (OnSubscriptionError != null)
                            await OnSubscriptionError.Invoke(e);
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (OnMessageError != null)
                await OnMessageError.Invoke(e);
        }
    }

    #region Subscriptions

    public async Task Subscribe<T>(string uri, Func<T, Task> onHandle, string type = "subscribe")
        => await Subscribe(uri, null, onHandle, type);

    public async Task Subscribe<T>(string uri, object? payload, Func<T, Task> onHandle, string type = "subscribe")
    {
        var payloadType = typeof(T);
        LgConnectSubscription? subscription;

        // We check if there is already a subscription for this event so we dont subscribe twice to the same event

        lock (Subscriptions)
        {
            subscription = Subscriptions.FirstOrDefault(x =>
                x.Uri == uri &&
                x.PayloadType == payloadType
            );
        }

        if (subscription == null)
        {
            subscription = new LgConnectSubscription()
            {
                Uri = uri,
                PayloadType = payloadType
            };

            lock (Subscriptions)
                Subscriptions.Add(subscription);

            // Let the tv know we want to listen to this
            var id = await Send(new BaseSubscribeRequest()
            {
                Uri = uri,
                Type = type,
                Payload = payload
            });

            subscription.PacketId = id;
        }

        subscription.Callbacks.Add(onHandle);
    }

    public Task Unsubscribe<T>(string uri)
    {
        var payloadType = typeof(T);

        lock (Subscriptions)
        {
            Subscriptions.RemoveAll(x =>
                x.Uri == uri &&
                x.PayloadType == payloadType
            );
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Requesting

    public async Task Request(string uri, object? payload = null)
    {
        var requestPacket = new BaseRequestRequest()
        {
            Uri = uri,
            Payload = payload,
            Type = "request"
        };

        await Send(requestPacket);
    }

    public async Task RequestWithResult<T>(string uri, object? payload, Func<T, Task> onHandle)
    {
        await Subscribe<T>(uri, payload, async parameter =>
        {
            await Unsubscribe<T>(uri);

            await onHandle.Invoke(parameter);
        }, "request");
    }

    #endregion

    #region Sending

    public async Task Send(string type, object payload)
    {
        var basePaket = new BasePacket()
        {
            Type = type,
            Payload = payload
        };

        await Send(basePaket);
    }

    public async Task Send(string customId, BasePacket packet)
    {
        packet.Id = Prefix + customId;

        // Serialize and encode
        var json = JsonSerializer.Serialize(
            packet,
            packet.GetType() // This makes sure that the correct type is used of serialisation
        );

        var bytes = Encoding.UTF8.GetBytes(json);
        await Send(bytes);
    }

    public async Task<string> Send(BasePacket packet)
    {
        // Set packet id
        var id = Formatter.IntToStringWithLeadingZeros(PacketCounter, 5);
        PacketCounter++;

        await Send(id, packet);

        return packet.Id;
    }

    public async Task Send(byte[] buffer)
    {
        if (WebSocket == null)
            throw new ArgumentNullException(nameof(WebSocket), "Websocket is not initialized");

        try
        {
            var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(3)
            );

            await WebSocket.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                WebSocketMessageFlags.EndOfMessage,
                cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            if (WebSocket.State == WebSocketState.Open)
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
            else
                await WebSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }

    #endregion

    private async Task SendInitialSubscriptions()
    {
        await Subscribe<PowerStateResponse>("ssap://com.webos.service.tvpower/power/getPowerState",
            new PowerStateRequest()
            {
                Subscribe = true
            },
            async response =>
            {
                ScreenOn = response.State.Equals(
                    "Active",
                    StringComparison.InvariantCultureIgnoreCase
                );

                if (OnScreenStateChanged != null)
                    await OnScreenStateChanged.Invoke(ScreenOn);
            }
        );

        await Subscribe<CurrentChannelResponse>("ssap://tv/getCurrentChannel", async response =>
        {
            if (int.TryParse(response.ChannelNumber, out var parsedNumber))
                Channel = parsedNumber;
            else
                Channel = -1;

            if (OnChannelChanged != null)
                await OnChannelChanged.Invoke(Channel);
        });
        
        await Subscribe<ForegroundAppInfoResponse>("ssap://com.webos.applicationManager/getForegroundAppInfo", async response =>
        {
            if(string.IsNullOrEmpty(response.AppId))
                return;

            var input = response.AppId switch
            {
                "com.webos.app.livetv" => LgConnectInput.LiveTv,
                "com.webos.app.browser" => LgConnectInput.Browser,
                "com.webos.app.hdmi1" => LgConnectInput.Hdmi1,
                "com.webos.app.hdmi2" => LgConnectInput.Hdmi2,
                "com.webos.app.hdmi3" => LgConnectInput.Hdmi3,
                _ => LgConnectInput.Unknown
            };

            Input = input;
                                
            if(OnInputChanged != null)
                await OnInputChanged.Invoke(input);
        });

        await Subscribe<AudioStatusResponse>("ssap://audio/getStatus", async response =>
        {
            Volume = response.Volume;

            if (OnVolumeChanged != null)
                await OnVolumeChanged.Invoke(Volume);
        });
        
        await Subscribe<MuteStatusResponse>("ssap://audio/getMute", async response =>
        {
            Mute = response.Mute;

            if (OnMuteChanged != null)
                await OnMuteChanged.Invoke(response.Mute);
        });
    }

    private async Task UpdateState(LgConnectState state)
    {
        State = state;

        if (OnStateChanged != null)
            await OnStateChanged.Invoke(State);
    }
}