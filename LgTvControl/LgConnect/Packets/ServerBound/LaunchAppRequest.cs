using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class LaunchAppRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}