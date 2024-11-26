using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LgTvControl;
using LgTvControl.Telnet;
using LgTvControl.Websocket;
using LgTvControl.Websocket.Enums;
using LgTvControl.Websocket.Payloads.ServerBound;
using Microsoft.Extensions.Logging;
using MoonCore.Extensions;
using MoonCore.Helpers;
/*
// Split MAC address into array of bytes
var macBytes = "b0:37:95:13:71:f1".Split(':')
    .Select(s => Convert.ToByte(s, 16))
    .ToArray();

// Create magic packet
var magicPacket = new byte[102];

// Add 6 bytes of 0xFF to the beginning of the packet
for (var i = 0; i < 6; i++)
{
    magicPacket[i] = 0xFF;
}

// Repeat target device's MAC address 16 times
for (var i = 6; i < 102; i += 6)
{
    Array.Copy(macBytes, 0, magicPacket, i, 6);
}

// Create UDP client and send magic packet to port 9 on the target device's subnet
using var udpClient = new UdpClient();
udpClient.EnableBroadcast = true;
await udpClient.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Parse("172.18.112.251"), 9));
*/
/*
var loggerFactory = new LoggerFactory();
loggerFactory.AddProviders(LoggerBuildHelper.BuildFromConfiguration(configuration =>
{
    configuration.Console.Enable = true;
    configuration.Console.EnableAnsiMode = true;
    configuration.FileLogging.Enable = false;
}));

var logger = loggerFactory.CreateLogger("Test");

var tv = new TelnetTvClient(logger, "172.27.66.28");

while (true)
{
    var key = Console.ReadKey();

    Console.WriteLine(JsonSerializer.Serialize(key));

    switch (key.Key)
    {
        case ConsoleKey.UpArrow:
            await tv.Send(TelnetTvCommand.Up);
            break;

        case ConsoleKey.DownArrow:
            await tv.Send(TelnetTvCommand.Down);
            break;

        case ConsoleKey.LeftArrow:
            await tv.Send(TelnetTvCommand.Left);
            break;

        case ConsoleKey.RightArrow:
            await tv.Send(TelnetTvCommand.Right);
            break;

        case ConsoleKey.Enter:
            await tv.Send(TelnetTvCommand.Enter);
            break;

        case ConsoleKey.Backspace:
            await tv.Send(TelnetTvCommand.Return);
            break;

        case ConsoleKey.M:
            await tv.Send(TelnetTvCommand.Menu);
            break;
    }
}*/

var loggerFactory = new LoggerFactory();
loggerFactory.AddProviders(LoggerBuildHelper.BuildFromConfiguration(configuration =>
{
    configuration.Console.Enable = true;
    configuration.Console.EnableAnsiMode = true;
    configuration.FileLogging.Enable = false;
}));

var logger = loggerFactory.CreateLogger("Test");

// 172.27.13.9
// 172.27.69.50
// 172.27.21.53

var tv = new TvClient(logger, "172.27.21.53", "");

tv.AcceptMode = TvPairAcceptMode.RightEnter;

tv.OnCreatePairingRequest = async () =>
{
    var paringRequest = JsonSerializer.Deserialize<PairingRequest>(
        await File.ReadAllTextAsync("pairing.json")
    )!;

    paringRequest.ClientKey = "81cc63d0de8da3117473925398d782ce";

    return paringRequest;
};

tv.OnVolumeChanged += x =>
{
    logger.LogDebug("Vol: {vol}", x);
    return Task.CompletedTask;
};

tv.OnChannelChanged += async x =>
{
    
};

tv.OnWebSocketStateChanged += async state =>
{
    if (state == WebsocketTvState.Ready)
    {
        Task.Run(async () =>
        {
            await tv.ScreenOff();
            await tv.SetChannel(1);
        });
    }
};

await tv.Connect();

await Task.Delay(-1);