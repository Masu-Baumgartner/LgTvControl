using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ClientBound;

public class MuteStatusResponse
{
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
}