using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ClientBound;

public class MuteStatusResponse
{
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
}