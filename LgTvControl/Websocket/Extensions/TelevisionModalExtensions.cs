using LgTvControl.Websocket.Payloads.ServerBound;

namespace LgTvControl.Websocket.Extensions;

public static class TelevisionModalExtensions
{
    public static async Task OpenModal(this WebSocketTvClient client, string text)
    {
        await client.Request("ssap://system.notifications/createToast", new CreateToastRequest()
        {
            Message = text
        });
    }
}