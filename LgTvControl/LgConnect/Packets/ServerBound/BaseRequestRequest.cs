using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class BaseRequestRequest : BasePacket
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; }
}