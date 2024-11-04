namespace LgTvControl.Websocket.Models;

public class VolumeChangeEvent
{
    public int Volume { get; set; }
    public bool ViaRemote { get; set; }
}