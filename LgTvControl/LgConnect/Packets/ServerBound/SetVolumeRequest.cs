using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class SetVolumeRequest
{
    [JsonPropertyName("volume")]
    public int Volume { get; set; }
}