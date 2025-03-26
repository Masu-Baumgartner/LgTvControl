using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets;

public class BasePacket
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}

public class BasePacket<T>
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("payload")]
    public T Payload { get; set; }
}