## LgTvControl
 This library allows you to remote control your lg tv via their websocket and telnet api

Example usage:
````csharp
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
        await tv.Screenshot(async s => logger.LogDebug("Screenshot available at: {uri}", s));
    }
};

await tv.Connect();

await Task.Delay(-1);
````