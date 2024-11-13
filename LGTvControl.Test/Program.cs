using System.Text.Json;
using LgTvControl;
using LgTvControl.Telnet;
using LgTvControl.Websocket;
using LgTvControl.Websocket.Enums;
using LgTvControl.Websocket.Payloads.ServerBound;
using Microsoft.Extensions.Logging;
using MoonCore.Extensions;
using MoonCore.Helpers;

var loggerFactory = new LoggerFactory();
loggerFactory.AddProviders(LoggerBuildHelper.BuildFromConfiguration(configuration =>
{
    configuration.Console.Enable = true;
    configuration.Console.EnableAnsiMode = true;
    configuration.FileLogging.Enable = false;
}));

var logger = loggerFactory.CreateLogger("Test");
/*
var tv = new TelnetTvClient(logger, "172.27.69.50");

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

var tv = new TvClient(logger, "172.27.69.50", "");

tv.AcceptMode = TvPairAcceptMode.DownEnter;

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
    logger.LogDebug("Channel: {vol}", x);

    if (x == 1)
    {
        await tv.SetChannel(5);
        await tv.ShowToast("You are not allowed to change the channel. This incident has been logged");
    }
};

tv.OnWebSocketStateChanged += async state =>
{
    if (state == WebsocketTvState.Ready)
    {
        await tv.ShowToast("testy");
    }
};

await tv.Connect();

await Task.Delay(-1);