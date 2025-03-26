using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class PowerStateRequest
{
    [JsonPropertyName("subscribe")]
    public bool Subscribe { get; set; } = true;
}