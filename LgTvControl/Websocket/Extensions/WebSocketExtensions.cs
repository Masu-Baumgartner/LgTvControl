using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LgTvControl.Websocket.Extensions;

public static class WebSocketExtensions
{
    public static async Task SendObjectAsJson(this WebSocket socket, object data)
    {
        try
        {
            var timeOut = new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token;

            var jsonText = JsonSerializer.Serialize(data);
            
            var buffer = Encoding.UTF8.GetBytes(jsonText);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
                timeOut);
        }
        catch (OperationCanceledException)
        {
            if (socket.State == WebSocketState.Open)
                await socket.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
            else
                await socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }
}