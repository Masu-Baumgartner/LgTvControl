using System.Net;
using System.Net.Sockets;
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
    public event Func<int, Task> OnChannelChanged;
    public event Func<int, Task> OnVolumeChanged;
    public event Func<string, Task> OnUnknownPacketReceived;
    
    // Proxy properties
    public WebsocketTvState WebsocketState => WebSocket.State;
    public bool TelnetConnected => Telnet.IsConnected;
    public int Volume => WebSocket.CurrentVolume;
    public int Channel => WebSocket.CurrentChannel;

    private readonly ILogger Logger;
    private readonly string Host;
    private readonly string MacAddress;

    private readonly TelnetTvClient Telnet;
    private readonly WebSocketTvClient WebSocket;

    public TvClient(ILogger logger, string host, string macAddress, int websocketPort = 3000, int telnetPort = 9761)
    {
        Logger = logger;
        Host = host;
        MacAddress = macAddress;

        WebSocket = new(Logger, Host, websocketPort);
        Telnet = new(Logger, Host, telnetPort);

        InitWebSocket();
    }

    private void InitWebSocket()
    {
        WebSocket.OnStateChanged += async state =>
        {
            if (state == WebsocketTvState.Connected)
            {
                PairingRequest? request = null;

                if (OnCreatePairingRequest != null)
                    request = await OnCreatePairingRequest.Invoke();

                request = request ?? new();

                await WebSocket.Pair(request);
            }
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
                    await Telnet.Send(TelnetTvCommand.Down);
                    await Telnet.Send(TelnetTvCommand.Enter);
                }
                else if(AcceptMode == TvPairAcceptMode.RightEnter)
                {
                    await Telnet.Send(TelnetTvCommand.Right);
                    await Telnet.Send(TelnetTvCommand.Enter);
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
        await Telnet.Connect();
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
        await WebSocket.RequestWithResult<OneShotResponse>("ssap://tv/executeOneShot", null, async response =>
        {
            await onHandle.Invoke(response.ImageUri);
        });
    }

    public async Task ShowToast(string message)
    {
        await WebSocket.Request("system.notifications/createToast", new CreateToastRequest()
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

    public async Task Disconnect()
    {
        Logger.LogTrace("Disconnecting from television");

        await WebSocket.Stop();
        await Telnet.Disconnect();
    }
}