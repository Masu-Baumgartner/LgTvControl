using LgTvControl.Websocket.Payloads.ServerBound;

namespace LgTvControl.Websocket.Extensions;

public static class TelevisionChannelExtensions
{
    public static async Task SetChannel(this WebSocketTvClient client, int channel)
    {
        await client.Request("ssap://tv/openChannel", new OpenChannelRequest()
        {
            ChannelNumber = channel.ToString()
        });
    }
}