using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class SetMuteRequest
{
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
}