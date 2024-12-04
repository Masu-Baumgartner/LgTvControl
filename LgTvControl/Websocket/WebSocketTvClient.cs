using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LgTvControl.Websocket.Enums;
using LgTvControl.Websocket.Extensions;
using LgTvControl.Websocket.Models;
using LgTvControl.Websocket.Packets;
using LgTvControl.Websocket.Payloads.ClientBound;
using LgTvControl.Websocket.Payloads.ServerBound;
using Microsoft.Extensions.Logging;
using MoonCore.Helpers;

namespace LgTvControl.Websocket;

public partial class WebSocketTvClient
{
    public ClientWebSocket Connection { get; set; }
    public WebsocketTvState State { get; set; }
    public int CurrentChannel { get; set; } = -1;
    public int CurrentVolume { get; set; } = -1;
    public bool CurrentScreenState { get; set; }

    public event Func<WebsocketTvState, Task> OnStateChanged;
    public event Func<string, Task> OnClientKeyChanged;
    public event Func<bool, Task> OnScreenStateChanged;
    public event Func<int, Task> OnChannelChanged;
    public event Func<VolumeChangeEvent, Task> OnVolumeChanged;
    public event Func<string, Task> OnUnknownPacketReceived;

    private readonly ILogger Logger;
    private readonly string IpAddress;
    private readonly int Port;

    private const string Prefix = "5d3ed79";

    private int PacketCounter = 1;
    private CancellationTokenSource Cancellation = new();

    private List<WebsocketTvSubscription> Subscriptions = new();

    public WebSocketTvClient(ILogger logger, string ipAddress, int port = 3000)
    {
        Logger = logger;
        IpAddress = ipAddress;
        Port = port;
    }

    public Task Start()
    {
        Cancellation = new();

        Task.Run(Loop);
        //Task.Run(KeepAliveLoop);

        return Task.CompletedTask;
    }

    private async Task Loop()
    {
        while (!Cancellation.IsCancellationRequested)
        {
            try
            {
                await UpdateState(WebsocketTvState.Offline);

                Connection = new ClientWebSocket();

                Connection.Options.SetBuffer(1016, 1016);
                Connection.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);

                var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

                try
                {
                    await Connection.ConnectAsync(new Uri($"ws://{IpAddress}:{Port}"), connectTimeout);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    Logger.LogTrace("Unable to connect to television (soft error): {e}", e);

                    // Wait a bit to prevent spamming
                    await Task.Delay(TimeSpan.FromSeconds(3));

                    continue;
                }

                await UpdateState(WebsocketTvState.Connected);

                while (Connection.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024];

                    try
                    {
                        var receiveResult = await Connection.ReceiveAsync(buffer, Cancellation.Token);

                        var resizedBuffer = new byte[receiveResult.Count];
                        Array.Copy(buffer, resizedBuffer, resizedBuffer.Length);

                        var text = Encoding.UTF8.GetString(resizedBuffer);

                        if (string.IsNullOrEmpty(text))
                            continue;

                        Logger.LogTrace("Received raw text: {text}", text);

                        var basePacket = JsonSerializer.Deserialize<BasePacket>(text);

                        if (basePacket == null)
                            continue;

                        if (basePacket.Type == "registered")
                        {
                            // Clear previous subscriptions
                            lock (Subscriptions)
                            {
                                Subscriptions.Clear();
                            }

                            await UpdateState(WebsocketTvState.Ready);

                            var pairingResponse = JsonSerializer.Deserialize<TypedBasePacket<PairingResponse>>(text);

                            if (pairingResponse == null)
                            {
                                Logger.LogWarning("Unable to parse pairing response");
                                continue;
                            }

                            if (OnClientKeyChanged != null)
                                await OnClientKeyChanged.Invoke(pairingResponse.Payload.ClientKey);

                            await Subscribe<PowerStateResponse>("ssap://com.webos.service.tvpower/power/getPowerState",
                                new PowerStateRequest()
                                {
                                    Subscribe = true
                                },
                                async response =>
                                {
                                    CurrentScreenState = response.State.Equals(
                                        "Active",
                                        StringComparison.InvariantCultureIgnoreCase
                                    );

                                    if (OnScreenStateChanged == null)
                                        return;

                                    await OnScreenStateChanged.Invoke(CurrentScreenState);
                                }
                            );

                            await Subscribe<CurrentChannelResponse>("ssap://tv/getCurrentChannel", async response =>
                            {
                                if (!int.TryParse(response.ChannelNumber, out var channel))
                                    CurrentChannel = 0;
                                else
                                    CurrentChannel = channel;

                                if (OnChannelChanged == null)
                                    return;

                                await OnChannelChanged.Invoke(CurrentChannel);
                            });

                            await Subscribe<AudioStatusResponse>("ssap://audio/getStatus", async response =>
                            {
                                CurrentVolume = response.Volume;

                                bool viaRemote;

                                if (string.IsNullOrEmpty(response.VolumeStatus.Cause))
                                    viaRemote = false;
                                else
                                    viaRemote = response.VolumeStatus.Cause != "setVolume";

                                if (OnVolumeChanged == null)
                                    return;

                                await OnVolumeChanged.Invoke(new VolumeChangeEvent()
                                {
                                    Volume = CurrentVolume,
                                    ViaRemote = viaRemote
                                });
                            });
                        }
                        else if (text.Contains("pairingType\":\"PROMPT"))
                        {
                            Logger.LogTrace("Waiting for pairing request to be accepted");
                            await UpdateState(WebsocketTvState.Pairing);
                        }
                        else if (text.Contains("403 too many pairing requests"))
                        {
                            Logger.LogTrace("TV is reporting too many connection attempts. Closing connection");

                            // Let's wait a bit until we retry as we dont want to open too many connections
                            await Task.Delay(TimeSpan.FromSeconds(3), Cancellation.Token);

                            await CloseCurrentSocket();
                        }
                        else if (text.Contains("403 Error!! power state"))
                        {
                            Logger.LogTrace("TV is reporting an invalid power state. Closing connection");
                            await CloseCurrentSocket();

                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        else
                        {
                            // Handle subscriptions
                            var packetId = basePacket.Id.Replace(Prefix, "");
                            WebsocketTvSubscription? subscription;

                            lock (Subscriptions)
                            {
                                subscription = Subscriptions
                                    .FirstOrDefault(x => x.PacketId == packetId);
                            }

                            if (subscription == null)
                            {
                                if (OnUnknownPacketReceived == null)
                                    continue;

                                await OnUnknownPacketReceived.Invoke(text);
                            }
                            else
                            {
                                var payloadPacket = JsonSerializer.Deserialize(
                                    text,
                                    typeof(TypedBasePacket<>).MakeGenericType(subscription.PayloadType)
                                );

                                if (payloadPacket == null)
                                {
                                    Logger.LogWarning("Unable to decode subscription packet");
                                    continue;
                                }

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
                                        Logger.LogError(
                                            "An unhandled error occured while calling callback function for {uri}: {e}",
                                            subscription.Uri, e
                                        );
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException e)
                    {
                        Logger.LogWarning("An error occured while deserializing json packet: {e}", e);
                    }
                    catch (WebSocketException e)
                    {
                        Logger.LogTrace("Websocket error occured: {e}", e);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("An unhandled error occured while processing loop: {e}", e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("An error occured in websocket connection loop: {e}", e);
            }
        }
    }

    public Task Stop()
    {
        Cancellation.Cancel();

        return Task.CompletedTask;
    }

    public async Task CloseCurrentSocket()
    {
        if (Connection.State == WebSocketState.Open)
            await Connection.CloseOutputAsync(WebSocketCloseStatus.Empty, null, Cancellation.Token);
    }

    public async Task Pair(PairingRequest request)
        => await SendBasePacket("register", request);

    public async Task KeepAliveLoop()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        while (!Cancellation.IsCancellationRequested)
        {
            var keepAliveTimeout = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), keepAliveTimeout.Token);

                    if (!keepAliveTimeout.IsCancellationRequested)
                    {
                        Logger.LogTrace("Reached keep alive timeout. Reconnecting");
                        await CloseCurrentSocket();
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.LogError("An error occured while handling keep alive occured: {e}", e);
                }
            });

            await RequestWithResult<SystemInfoResponse>("ssap://system/getSystemInfo", null, async response =>
            {
                if (!keepAliveTimeout.IsCancellationRequested)
                    await keepAliveTimeout.CancelAsync();
            });

            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }

    private async Task UpdateState(WebsocketTvState state)
    {
        State = state;

        if (OnStateChanged == null)
            return;

        await OnStateChanged.Invoke(State);
    }

    #region Subscriptions

    public async Task Subscribe<T>(string uri, Func<T, Task> onHandle, string type = "subscribe")
        => await Subscribe(uri, null, onHandle, type);

    public async Task Subscribe<T>(string uri, object? payload, Func<T, Task> onHandle, string type = "subscribe")
    {
        var payloadType = typeof(T);
        WebsocketTvSubscription? subscription;

        lock (Subscriptions)
        {
            subscription = Subscriptions.FirstOrDefault(x =>
                x.Uri == uri &&
                x.PayloadType == payloadType
            );
        }

        if (subscription == null)
        {
            var packetId = GetNextPackedId();

            subscription = new WebsocketTvSubscription()
            {
                PacketId = packetId,
                Uri = uri,
                PayloadType = payloadType
            };

            lock (Subscriptions)
                Subscriptions.Add(subscription);

            // Let the tv know we want to listen to this
            await SendBasePacket(packetId, new BaseSubscribePacket()
            {
                Uri = uri,
                Type = type,
                Payload = payload
            });
        }

        subscription.Callbacks.Add(onHandle);

        Logger.LogTrace("Subscribed to {uri} ({type})", uri, payloadType);
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

        Logger.LogTrace("Unsubscribed from {uri} ({type})", uri, payloadType);

        return Task.CompletedTask;
    }

    #endregion

    public async Task RequestWithResult<T>(string uri, object? payload, Func<T, Task> onHandle)
    {
        await Subscribe<T>(uri, payload, async parameter =>
        {
            await Unsubscribe<T>(uri);

            await onHandle.Invoke(parameter);
        }, "request");
    }

    #region Packets

    public async Task<string> SendBasePacket(string type, object payload)
    {
        var basePacket = new BasePacket()
        {
            Type = type,
            Payload = payload
        };

        return await SendBasePacket(basePacket);
    }

    public async Task SendBasePacket(string customId, BasePacket packet)
    {
        packet.Id = Prefix + customId;
        await Connection.SendObjectAsJson(packet);
    }

    public async Task<string> SendBasePacket(BasePacket packet)
    {
        var packetId = GetNextPackedId();
        await SendBasePacket(packetId, packet);

        return packetId;
    }

    public async Task Request(string uri, object? payload = null)
    {
        var requestPacket = new BaseRequestPacket()
        {
            Uri = uri,
            Payload = payload,
            Type = "request"
        };

        await SendBasePacket(requestPacket);
    }

    private string GetNextPackedId()
    {
        var id = Formatter.IntToStringWithLeadingZeros(PacketCounter, 5);
        PacketCounter += 1;

        return id;
    }

    #endregion
}