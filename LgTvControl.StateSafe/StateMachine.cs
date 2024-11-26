using Microsoft.Extensions.Logging;

namespace LgTvControl.StateSafe;

public class StateMachine<T> where T : struct, Enum
{
    public T CurrentState { get; private set; }

    private readonly List<StateTransition<T>> Transitions = new();
    private readonly List<StateEvent<T>> StateEvents = new();
    private readonly ILogger Logger;
    private SpinLock Lock = new();
    private bool IsTransitioning = false;

    public StateMachine(T initialState, ILogger logger)
    {
        CurrentState = initialState;
        Logger = logger;
    }

    public Task TransitionTo(T targetState)
    {
        var transition = Transitions.FirstOrDefault(
            x =>
                x.BaseState.Equals(CurrentState) &&
                x.SuccessState.Equals(targetState)
        );

        if (transition == null)
            throw new ArgumentException($"No transition from {CurrentState} to {targetState} found");

        Task.Run(async () => await ExecuteTransition(transition));
        
        return Task.CompletedTask;
    }

    private async Task ExecuteTransition(StateTransition<T> transition)
    {
        Lock.Enter(ref IsTransitioning);

        if (!transition.BaseState.Equals(CurrentState))
        {
            Logger.LogError("Unable to transition to {target} from {current}", transition.SuccessState, CurrentState);

            Lock.Exit();
            return;
        }

        CancellationToken taskCancellation;
        CancellationTokenSource? cancellationTokenSource = null;

        if (transition.TimeoutState != null && transition.Timeout.HasValue)
        {
            cancellationTokenSource = new CancellationTokenSource();
            taskCancellation = cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(transition.Timeout.Value, cancellationTokenSource.Token);

                    if (cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    await cancellationTokenSource.CancelAsync();
                    
                    Lock.Exit();
                    await TransitionTo(transition.TimeoutState.Value);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.LogError("An unhandled error occured while processing timeout handling: {e}", e);
                }
            });
        }
        else
            taskCancellation = CancellationToken.None;

        try
        {
            await transition.Action.Invoke(taskCancellation);

            // Handle timeout
            if (cancellationTokenSource != null)
                await cancellationTokenSource.CancelAsync();
        }
        catch (Exception e)
        {
            // Handle timeout
            if (cancellationTokenSource != null)
                await cancellationTokenSource.CancelAsync();

            Logger.LogDebug(
                "An error occured while executing action to transition from {current} to {to}: {e}",
                CurrentState,
                transition.SuccessState,
                e
            );

            if (transition.ErrorState.HasValue)
            {
                try
                {
                    Lock.Exit();

                    await TransitionTo(transition.ErrorState.Value);
                    return;
                }
                catch (Exception exception)
                {
                    Logger.LogError("An error occured while transitioning to error state: {e}", exception);
                }
            }
            
            Lock.Exit();
            return;
        }

        CurrentState = transition.SuccessState;
        Lock.Exit();

        var events = StateEvents
            .Where(x => x.State.Equals(CurrentState))
            .ToArray();
        
        foreach (var stateEvent in events)
        {
            try
            {
                await stateEvent.Action.Invoke();
            }
            catch (Exception e)
            {
                Logger.LogError("An error occured while handling state event for {current}: {e}", CurrentState, e);
            }
        }
    }

    #region State Events add methods

    public void AddStateEvent(T state, Func<Task> action)
    {
        StateEvents.Add(new StateEvent<T>()
        {
            State = state,
            Action = action
        });
    }

    #endregion

    #region Transitions add methods

    public void AddTransition(T baseState, T successState, Func<CancellationToken, Task> action)
        => AddTransition(baseState, successState, null, null, null, action);

    public void AddTransition(T baseState, T successState, T errorState, Func<CancellationToken, Task> action)
        => AddTransition(baseState, successState, errorState, null, null, action);

    public void AddTransition(
        T baseState,
        T successState,
        T? errorState,
        T? timeoutState,
        TimeSpan? timeout,
        Func<CancellationToken, Task> action
    )
    {
        Transitions.Add(new StateTransition<T>()
        {
            Action = action,
            BaseState = baseState,
            SuccessState = successState,
            ErrorState = errorState,
            TimeoutState = timeoutState,
            Timeout = timeout
        });
    }

    #endregion
}

class StateTransition<T> where T : struct, Enum
{
    public T BaseState { get; set; }
    public T SuccessState { get; set; }
    public T? ErrorState { get; set; }
    public T? TimeoutState { get; set; }
    public TimeSpan? Timeout { get; set; }
    public Func<CancellationToken, Task> Action { get; set; }
}

class StateEvent<T>
{
    public T State { get; set; }
    public Func<Task> Action { get; set; }
}