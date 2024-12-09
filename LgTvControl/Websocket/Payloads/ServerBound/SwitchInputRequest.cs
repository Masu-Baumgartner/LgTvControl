using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class SwitchInputRequest
{
    [JsonPropertyName("inputId")]
    public string InputId { get; set; }
}