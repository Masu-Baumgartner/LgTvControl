using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class PowerStateRequest
{
    [JsonPropertyName("subscribe")]
    public bool Subscribe { get; set; } = true;
}