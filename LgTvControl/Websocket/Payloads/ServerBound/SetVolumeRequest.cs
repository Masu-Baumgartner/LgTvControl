using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class SetVolumeRequest
{
    [JsonPropertyName("volume")]
    public int Volume { get; set; }
}