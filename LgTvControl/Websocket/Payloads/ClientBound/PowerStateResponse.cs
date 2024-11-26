using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ClientBound;

public class PowerStateResponse
{
    [JsonPropertyName("state")]
    public string State { get; set; }
}