using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ClientBound;

public class PowerStateResponse
{
    [JsonPropertyName("state")]
    public string State { get; set; }
}