using System.Text.Json;
using LgTvControl.IpControl;
using LgTvControl.LgConnect;
using LgTvControl.LgConnect.Packets.ServerBound;

var ipControl = new IpControlClient("172.27.69.50", 9761, "SGDJPXMZ");

ipControl.OnError += async s => Console.WriteLine(s);

var lgConnect = new LgConnectClient("172.27.69.50", 3000);

lgConnect.OnWebsocketError += async e => Console.WriteLine(e);
lgConnect.OnMessageError += async e => Console.WriteLine(e);
lgConnect.OnSubscriptionError += async e => Console.WriteLine(e);

lgConnect.OnVolumeChanged += async i => Console.WriteLine(i);

lgConnect.OnStateChanged += async state =>
{
    Console.WriteLine($"State: {state}");

    if (state == LgConnectState.Pairing)
    {
        await Task.Delay(500);
        await ipControl.SendKey(IpControlKey.ArrowDown);
        await Task.Delay(500);
        await ipControl.SendKey(IpControlKey.Ok);
    }
    else if (state == LgConnectState.Connected)
    {
        var json = File.ReadAllText("pairing.json");
        var paringRequest = JsonSerializer.Deserialize<PairingRequest>(json)!;

        await lgConnect.Send("register", paringRequest);
    }
};

await lgConnect.Start();

await Task.Delay(-1);