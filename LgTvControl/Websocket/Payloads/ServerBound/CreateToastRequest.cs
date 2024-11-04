using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class CreateToastRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
}