using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ClientBound;

public class ForegroundAppInfoResponse
{
    [JsonPropertyName("appId")]
    public string AppId { get; set; }
}