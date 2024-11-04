using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ClientBound;

public class OneShotResponse
{
    [JsonPropertyName("imageUri")]
    public string ImageUri { get; set; }
}