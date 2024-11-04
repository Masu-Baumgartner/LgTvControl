using LgTvControl.Websocket.Payloads.ServerBound;

namespace LgTvControl.Websocket.Extensions;

public static class TelevisionAudioExtensions
{
    public static async Task SetVolume(this WebSocketTvClient client, int volume)
    {
        await client.Request("ssap://audio/setVolume", new SetVolumeRequest()
        {
            Volume = volume
        });
    }
}