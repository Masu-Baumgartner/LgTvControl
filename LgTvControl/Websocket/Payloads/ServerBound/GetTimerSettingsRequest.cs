using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public class GetTimerSettingsRequest
{
    [JsonPropertyName("keys")]
    public string[] Keys { get; set; } = [];

    [JsonPropertyName("isSubscription")] public bool IsSubscription { get; set; } = false;
}