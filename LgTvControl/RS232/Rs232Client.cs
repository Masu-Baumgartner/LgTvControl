using PrimS.Telnet;

namespace LgTvControl.RS232;

public class Rs232Client
{
    public bool IsConnected => Client?.IsConnected ?? false;
    
    private readonly string Host;
    private readonly int Port;

    private CancellationTokenSource? Cancellation;
    private Client? Client;
    private TcpByteStream? NetworkStream;

    public Rs232Client(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public Task Connect()
    {
        NetworkStream = new TcpByteStream(Host, Port);
        Cancellation = new();
        Client = new Client(NetworkStream, Cancellation.Token);
        
        return Task.CompletedTask;
    }

    public async Task SendCommand(string command)
    {
        if (!IsConnected)
            await Connect();

        await Client!.WriteLineRfc854Async(command);
    }

    public async Task SendCommand(Rs232Command command)
    {
        var commandStr = CommandToString(command);
        await SendCommand(commandStr);
    }

    public async Task Disconnect()
    {
        if(Client == null || Cancellation == null || NetworkStream == null)
            return;

        await Cancellation.CancelAsync();
        
        Client.Dispose();
        NetworkStream.Close();
        NetworkStream.Dispose();
    }

    private string CommandToString(Rs232Command command)
    {
        return command switch
        {
            Rs232Command.ChannelUp => "mc 1 00",
            Rs232Command.ChannelDown => "mc 1 01",
            Rs232Command.VolumeUp => "mc 1 02",
            Rs232Command.VolumeDown => "mc 1 03",
            Rs232Command.Left => "mc 1 07",
            Rs232Command.Right => "mc 1 06",
            Rs232Command.Up => "mc 1 40",
            Rs232Command.Down => "mc 1 41",
            Rs232Command.Menu => "mc 1 43",
            Rs232Command.Enter => "mc 1 44",
            Rs232Command.Return => "mc 1 28",
            _ => ""
        };
    }
}