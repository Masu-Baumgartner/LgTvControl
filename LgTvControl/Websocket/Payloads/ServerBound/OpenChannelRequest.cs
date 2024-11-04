using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class OpenChannelRequest
{
    [JsonPropertyName("channelNumber")]
    public string ChannelNumber { get; set; } 
}