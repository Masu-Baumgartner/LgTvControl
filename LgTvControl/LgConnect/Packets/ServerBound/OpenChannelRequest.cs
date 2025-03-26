using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class OpenChannelRequest
{
    [JsonPropertyName("channelNumber")]
    public string ChannelNumber { get; set; } 
}