using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class SwitchInputRequest
{
    [JsonPropertyName("inputId")]
    public string InputId { get; set; }
}