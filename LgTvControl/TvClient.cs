using System.Net;
using System.Net.Sockets;
using System.Text;
using LgTvControl.Telnet;
using LgTvControl.Websocket;
using LgTvControl.Websocket.Enums;
using LgTvControl.Websocket.Payloads.ClientBound;
using LgTvControl.Websocket.Payloads.ServerBound;
using Microsoft.Extensions.Logging;

namespace LgTvControl;

public class TvClient
{
    public Func<Task<PairingRequest>>? OnCreatePairingRequest { get; set; }
    public TvPairAcceptMode AcceptMode { get; set; } = TvPairAcceptMode.Never;

    // Events
    public event Func<WebsocketTvState, Task> OnWebSocketStateChanged;
    public event Func<string, Task> OnClientKeyChanged;
    public event Func<bool, Task> OnScreenStateChanged;
    public event Func<int, Task> OnChannelChanged;
    public event Func<int, Task> OnVolumeChanged;
    public event Func<bool, Task> OnMuteChanged;
    public event Func<TelevisionInput, Task> OnInputChanged;
    public event Func<string, Task> OnUnknownPacketReceived;

    // Proxy properties
    public WebsocketTvState WebsocketState => WebSocket.State;
    public bool TelnetConnected => Telnet.IsConnected;
    public int Volume => WebSocket.CurrentVolume;
    public int Channel => WebSocket.CurrentChannel;
    public bool ScreenState => WebSocket.CurrentScreenState;
    public TelevisionInput Input => WebSocket.CurrentInput;
    public bool Mute => WebSocket.CurrentMute;

    private readonly ILogger Logger;
    private readonly string Host;
    private readonly string MacAddress;

    private readonly TelnetTvClient Telnet;
    private readonly WebSocketTvClient WebSocket;

    private CancellationTokenSource? PairingTimeout;

    public TvClient(ILogger logger, string host, string macAddress, int websocketPort = 3000, int telnetPort = 9761)
    {
        Logger = logger;
        Host = host;
        MacAddress = macAddress;

        WebSocket = new(Logger, Host, websocketPort);
        Telnet = new(Host, telnetPort, Logger);

        InitWebSocket();
    }

    private void InitWebSocket()
    {
        WebSocket.OnStateChanged += async state =>
        {
            Logger.LogDebug("State: {state}", state);

            if (state == WebsocketTvState.Connected)
            {
                PairingRequest? request = null;

                if (OnCreatePairingRequest != null)
                    request = await OnCreatePairingRequest.Invoke();

                request = request ?? new();

                await WebSocket.Pair(request);

                // Setup timeout for pairing
                PairingTimeout = new();
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), PairingTimeout.Token);

                        // Ignore every non pairing state
                        if (WebSocket.State != WebsocketTvState.Pairing)
                            return;

                        Logger.LogTrace("Reconnecting websocket after pairing timeout");
                        await WebSocket.CloseCurrentSocket();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("An unknown error occured while handling pairing timeout: {e}", e);
                    }
                });
            }
            else if (state == WebsocketTvState.Offline)
            {
                if (Telnet.IsConnected)
                    await Telnet.Disconnect();
            }

            if (PairingTimeout != null)
            {
                if (state == WebsocketTvState.Ready)
                {
                    // Cancel timeout as we successfully paired
                    await PairingTimeout.CancelAsync();
                }
                else if (state == WebsocketTvState.Offline)
                {
                    // Cancel when the connection dropped
                    await PairingTimeout.CancelAsync();
                }
            }
        };

        WebSocket.OnScreenStateChanged += async state =>
        {
            if (OnScreenStateChanged != null)
                await OnScreenStateChanged.Invoke(state);
        };

        WebSocket.OnStateChanged += async state =>
        {
            if (OnWebSocketStateChanged != null)
                await OnWebSocketStateChanged.Invoke(state);

            if (state == WebsocketTvState.Pairing && AcceptMode != TvPairAcceptMode.Never)
            {
                Logger.LogTrace("Auto accepting with accept mode: {mode}", AcceptMode);

                if (AcceptMode == TvPairAcceptMode.DownEnter)
                {
                    await Task.Delay(1000);

                    await Telnet.Connect();

                    await Telnet.SendCommand(TelnetTvCommand.Down);

                    await Task.Delay(1000);

                    await Telnet.SendCommand(TelnetTvCommand.Enter);
                }
                else if (AcceptMode == TvPairAcceptMode.RightEnter)
                {
                    await Task.Delay(1000);

                    await Telnet.Connect();

                    await Telnet.SendCommand(TelnetTvCommand.Right);

                    await Task.Delay(1000);

                    await Telnet.SendCommand(TelnetTvCommand.Enter);
                }
            }
        };

        WebSocket.OnChannelChanged += async channel =>
        {
            if (OnChannelChanged != null)
                await OnChannelChanged.Invoke(channel);
        };

        WebSocket.OnVolumeChanged += async eventData =>
        {
            if (OnVolumeChanged != null)
                await OnVolumeChanged.Invoke(eventData.Volume);
        };
        
        WebSocket.OnInputChanged += async eventData =>
        {
            if (OnInputChanged != null)
                await OnInputChanged.Invoke(eventData);
        };
        
        WebSocket.OnMuteChanged += async eventData =>
        {
            if (OnMuteChanged != null)
                await OnMuteChanged.Invoke(eventData);
        };

        WebSocket.OnClientKeyChanged += async clientKey =>
        {
            if (OnClientKeyChanged != null)
                await OnClientKeyChanged.Invoke(clientKey);
        };

        WebSocket.OnUnknownPacketReceived += async packet =>
        {
            if (OnUnknownPacketReceived != null)
                await OnUnknownPacketReceived.Invoke(packet);
        };
    }

    public async Task Connect()
    {
        Logger.LogTrace("Connecting to television");

        await WebSocket.Start();
    }

    public async Task ScreenOn()
    {
        await WebSocket.Request("ssap://com.webos.service.tvpower/power/turnOnScreen");
    }

    public async Task ScreenOff()
    {
        await WebSocket.Request("ssap://com.webos.service.tvpower/power/turnOffScreen");
    }

    public async Task RequestPowerState(Func<string, Task> onCallback)
    {
        await WebSocket.RequestWithResult<PowerStateResponse>("ssap://com.webos.service.tvpower/power/getPowerState",
            null,
            async response => { await onCallback.Invoke(response.State); }
        );
    }

    public async Task SetVolume(int volume)
    {
        await WebSocket.Request("ssap://audio/setVolume", new SetVolumeRequest()
        {
            Volume = volume
        });
    }

    public async Task SetChannel(int channel)
    {
        await WebSocket.Request("ssap://tv/openChannel", new OpenChannelRequest()
        {
            ChannelNumber = channel.ToString()
        });
    }

    public async Task Screenshot(Func<string, Task> onHandle)
    {
        await WebSocket.RequestWithResult<OneShotResponse>("ssap://tv/executeOneShot", null,
            async response => { await onHandle.Invoke(response.ImageUri); });
    }

    public async Task ShowToast(string message)
    {
        await WebSocket.Request("ssap://system.notifications/createToast", new CreateToastRequest()
        {
            Message = message
        });
    }

    public async Task TurnOn()
    {
        // Split MAC address into array of bytes
        var macBytes = MacAddress.Split(':')
            .Select(s => Convert.ToByte(s, 16))
            .ToArray();

        // Create magic packet
        var magicPacket = new byte[102];

        // Add 6 bytes of 0xFF to the beginning of the packet
        for (var i = 0; i < 6; i++)
        {
            magicPacket[i] = 0xFF;
        }

        // Repeat target device's MAC address 16 times
        for (var i = 6; i < 102; i += 6)
        {
            Array.Copy(macBytes, 0, magicPacket, i, 6);
        }

        // Create UDP client and send magic packet to port 9 on the target device's subnet
        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        await udpClient.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Parse(Host), 9));
    }

    public async Task TurnOff()
        => await WebSocket.Request("ssap://system/turnOff");

    public async Task SendCommand(TelnetTvCommand command)
        => await Telnet.SendCommand(command);

    public async Task SendCommandRaw(string command)
        => await Telnet.SendCommand(command);
    
    public async Task SwitchToLiveTv()
    {
        await WebSocket.Request("ssap://com.webos.applicationManager/launch", new LaunchAppRequest()
        {
            Id = "com.webos.app.livetv"
        });
    }

    public async Task CloseWebBrowser()
    {
        await WebSocket.Request("ssap://system.launcher/close", new CloseAppRequest()
        {
            Id = "com.webos.app.browser"
        });
    }

    public async Task LaunchWebBrowser(string uri)
    {
        await WebSocket.Request("ssap://system.launcher/launch", new LaunchWebBrowserRequest()
        {
            Id = "com.webos.app.browser",
            Target = uri,
            Params = new()
            {
                Target = uri
            }
        });
    }

    public async Task SwitchInput(TelevisionInput input, string? parameter = null)
    {
        // Close current browser
        if (WebSocket.CurrentInput == TelevisionInput.Browser)
            await CloseWebBrowser();
        
        // Handle new input
        if (input == TelevisionInput.LiveTv)
            await SwitchToLiveTv();
        else if(input == TelevisionInput.Browser)
        {
            parameter = parameter ?? "https://google.com";
            await LaunchWebBrowser(parameter);
        }
        else
        {
            // Handle all other inputs
            var inputId = input switch
            {
                TelevisionInput.Hdmi1 => "HDMI_1",
                TelevisionInput.Hdmi2 => "HDMI_2",
                TelevisionInput.Hdmi3 => "HDMI_3",
                _ => ""
            };

            if (!string.IsNullOrEmpty(inputId)) // When a valid input id is found, switch to it
            {
                await WebSocket.Request("ssap://tv/switchInput", new SwitchInputRequest()
                {
                    InputId = inputId
                });
            }
        }
    }

    public async Task SetMute(bool mute)
    {
        await WebSocket.Request("ssap://audio/setMute", new SetMuteRequest()
        {
            Mute = mute
        });
    }

    public async Task Disconnect()
    {
        Logger.LogTrace("Disconnecting from television");

        await WebSocket.Stop();

        await Telnet.Disconnect();
    }
}