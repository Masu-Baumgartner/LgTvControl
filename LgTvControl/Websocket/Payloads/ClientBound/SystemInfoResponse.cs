using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ClientBound;

public class SystemInfoResponse
{
    [JsonPropertyName("modelName")]
    public string ModelName { get; set; }

    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; }
}