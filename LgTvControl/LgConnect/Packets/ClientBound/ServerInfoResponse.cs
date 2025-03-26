using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ClientBound;

public class ServerInfoResponse
{
    [JsonPropertyName("modelName")]
    public string ModelName { get; set; }

    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; }
}