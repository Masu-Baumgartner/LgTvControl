using System.Net.Sockets;

namespace LgTvControl.IpControl;

public class IpControlConnection
{
    public event Func<string, Task>? OnMessageReceived;

    public bool IsConnected => TcpClient.Connected;
    
    private readonly string Host;
    private readonly int Port;
    private readonly IpControlEncryption ControlEncryption;
    private readonly TcpClient TcpClient;
    private NetworkStream NetworkStream;

    private byte[] ReadBuffer;
    
    public IpControlConnection(string host, int port, string key, int bufferSize = 1024)
    {
        Host = host;
        Port = port;

        ControlEncryption = new(key);
        TcpClient = new();

        ReadBuffer = new byte[bufferSize];
    }

    public async Task Connect()
    {
        await TcpClient.ConnectAsync(Host, Port);
        NetworkStream = TcpClient.GetStream();

        NetworkStream.BeginRead(ReadBuffer, 0, ReadBuffer.Length, OnReadEnd, null);
    }

    public Task SendMessage(string message)
    {
        var encodedMessage = ControlEncryption.Encode(message);
        NetworkStream.BeginWrite(encodedMessage, 0, encodedMessage.Length, OnWriteEnd, null);
        
        return Task.CompletedTask;
    }

    public Task Disconnect()
    {
        TcpClient.Close();
        return Task.CompletedTask;
    }

    #region Async Result Handlers

    private void OnWriteEnd(IAsyncResult ar)
    {
        NetworkStream.EndWrite(ar);
        NetworkStream.Flush();
    }
    
    private void OnReadEnd(IAsyncResult ar)
    {
        var bytesRead = NetworkStream.EndRead(ar);
        var resizedBuffer = new byte[bytesRead];
        Array.Copy(ReadBuffer, resizedBuffer, bytesRead);
        
        if(TcpClient.Connected)
            NetworkStream.BeginRead(ReadBuffer, 0, ReadBuffer.Length, OnReadEnd, null);

        var decodedMessage = ControlEncryption.Decode(resizedBuffer);

        if (OnMessageReceived != null)
            OnMessageReceived.Invoke(decodedMessage).Wait();
    }

    #endregion
}