using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using PrimS.Telnet;

namespace LgTvControl.Telnet;

public class TelnetTvClient
{
    private readonly string IpAddress;
    private readonly int Port;
    private readonly ILogger Logger;

    private CancellationTokenSource? Cancellation;
    private PrimS.Telnet.Client? Client;
    private PrimS.Telnet.TcpByteStream? TcpByteStream;

    public bool IsConnected => TcpByteStream?.Connected ?? false;

    public TelnetTvClient(string ipAddress, int port, ILogger logger)
    {
        IpAddress = ipAddress;
        Port = port;
        Logger = logger;
    }

    public Task Connect()
    {
        TcpByteStream = new(IpAddress, Port);
        Cancellation = new CancellationTokenSource();
        Client = new Client(TcpByteStream, Cancellation.Token);
        
        return Task.CompletedTask;
    }

    public async Task SendCommand(string command)
    {
        await EnsureConnected();
        await Client!.WriteLineRfc854Async(command);
    }
    
    public async Task SendCommand(TelnetTvCommand command)
        => await SendCommand(ToCommandString(command));

    private async Task EnsureConnected()
    {
        if (TcpByteStream == null || Client == null || !Client.IsConnected)
            await Connect();
    }

    public async Task Disconnect()
    {
        if (Cancellation == null || Client == null || TcpByteStream == null)
            return;

        await Cancellation.CancelAsync();
        
        Client.Dispose();
        TcpByteStream.Dispose();
    }

    private string ToCommandString(TelnetTvCommand command)
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