namespace LgTvControl.LgConnect;

public class LgConnectSubscription
{
    public string PacketId { get; set; }
    public string Uri { get; set; }
    public List<object> Callbacks { get; set; } = new();
    public Type PayloadType { get; set; }
}