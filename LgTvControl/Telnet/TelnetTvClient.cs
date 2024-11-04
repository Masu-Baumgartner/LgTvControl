using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LgTvControl.Telnet;

public class TelnetTvClient
{
    public bool IsConnected => TcpClient?.Connected ?? false;
    
    private readonly string IpAddress;
    private readonly int Port;
    private readonly ILogger Logger;
    
    private TcpClient? TcpClient;
    
    public TelnetTvClient(ILogger logger, string ipAddress, int port = 9761)
    {
        Logger = logger;
        IpAddress = ipAddress;
        Port = port;
    }

    public async Task Connect()
    {
        await Disconnect();

        TcpClient = new();

        await TcpClient.ConnectAsync(IpAddress, Port);

        if (!TcpClient.Connected)
        {
            Logger.LogWarning("Lost connection");
            return;
        }
    }

    public Task Disconnect()
    {
        if(TcpClient == null)
            return Task.CompletedTask;

        if (TcpClient.Connected)
            TcpClient.Close();
        
        TcpClient.Dispose();
        TcpClient = null;
        
        return Task.CompletedTask;
    }

    public async Task Send(string command)
    {
        if (TcpClient == null || !TcpClient.Connected)
            await Connect();

        var networkStream = TcpClient!.GetStream();

        var encodedCommand = Encoding.UTF8.GetBytes(command + "\n");

        await networkStream.WriteAsync(encodedCommand);
        await networkStream.FlushAsync();
    }

    public Task Send(TelnetTvCommand command)
        => Send(EnumToCommand(command));

    private string EnumToCommand(TelnetTvCommand command)
    {
        return command switch
        {
            TelnetTvCommand.ChannelUp => "mc 1 00",
            TelnetTvCommand.ChannelDown => "mc 1 01",
            TelnetTvCommand.VolumeUp => "mc 1 02",
            TelnetTvCommand.VolumeDown => "mc 1 03",
            TelnetTvCommand.Left => "mc 1 07",
            TelnetTvCommand.Right => "mc 1 06",
            TelnetTvCommand.Up => "mc 1 40",
            TelnetTvCommand.Down => "mc 1 41",
            TelnetTvCommand.Menu => "mc 1 43",
            TelnetTvCommand.Enter => "mc 1 44",
            TelnetTvCommand.Return => "mc 1 28",
            _ => ""
        };
    }
}