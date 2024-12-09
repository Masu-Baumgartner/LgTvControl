using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class LaunchAppRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}