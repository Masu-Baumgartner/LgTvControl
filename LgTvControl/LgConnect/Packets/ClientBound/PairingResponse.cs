using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ClientBound;

public class PairingResponse
{
    [JsonPropertyName("client-key")] public string ClientKey { get; set; }
}