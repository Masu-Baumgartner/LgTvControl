using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class CreateToastRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
}