using System.Net.WebSockets;
using LgTvControl.Websocket.Enums;
using LgTvControl.Websocket.Models;
using MoonCore.Helpers;

namespace LgTvControl.Websocket;

public class TelevisionConnection
{
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public string MacAddress { get; set; }
    
    public WebSocket Connection { get; set; }
    public WebsocketTvState State { get; set; }
    public int CurrentChannel { get; set; } = -1;
    public int CurrentVolume { get; set; } = -1;
    public SmartEventHandler<WebsocketTvState> OnStateChanged { get; set; } = new();
    public SmartEventHandler<string> OnClientKeyChanged { get; set; } = new();
    public SmartEventHandler<int> OnChannelChanged { get; set; } = new();
    public SmartEventHandler<VolumeChangeEvent> OnVolumeChanged { get; set; } = new();
    public int PacketCounter { get; set; } = 1;
}