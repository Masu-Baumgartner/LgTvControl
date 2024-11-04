using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ClientBound;

public class PairingResponse
{
    [JsonPropertyName("client-key")] public string ClientKey { get; set; }
}