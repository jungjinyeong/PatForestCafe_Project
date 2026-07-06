using System;
using System.Collections.Generic;

public class StateMachine<TState> where TState : Enum
{
    private readonly Dictionary<TState, Action> mEnterActions = new Dictionary<TState, Action>();
    private readonly Dictionary<TState, Action> mExitActions = new Dictionary<TState, Action>();

    public TState Current { get; private set; }

    public void RegisterState(TState state, Action onEnter = null, Action onExit = null)
    {
        if (onEnter != null)
            mEnterActions[state] = onEnter;

        if (onExit != null)
            mExitActions[state] = onExit;
    }

    public void ChangeState(TState next)
    {
        if (mExitActions.TryGetValue(Current, out var onExit))
            onExit.Invoke();

        Current = next;

        if (mEnterActions.TryGetValue(next, out var onEnter))
            onEnter.Invoke();
    }
}
