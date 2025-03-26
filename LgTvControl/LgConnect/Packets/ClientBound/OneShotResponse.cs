using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ClientBound;

public class OneShotResponse
{
    [JsonPropertyName("imageUri")]
    public string ImageUri { get; set; }
}