using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ClientBound;

public class ForegroundAppInfoResponse
{
    [JsonPropertyName("appId")]
    public string AppId { get; set; }
}