using Microsoft.Extensions.Logging;

namespace LgTvControl.StateSafe;

public class LgTvClient
{
    private readonly StateMachine<TvClientState> StateMachine;
    private readonly ILogger Logger;

    public LgTvClient(ILogger logger)
    {
        Logger = logger;
        StateMachine = new(TvClientState.Offline, logger);
    }
    
    //public async Task Connect()
}