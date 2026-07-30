using System;
using System.Collections.Generic;
using Djurspel.Core;

namespace Djurspel.Game;

/// <summary>
/// Concrete state machine — handles state transitions and runs the main game loop.
/// The actual loop is delegated to GameLoop which runs on the GameWindow thread.
/// </summary>
public class GameStateMachine : IGameStateMachine
{
    private readonly IEventDispatcher _dispatcher;
    private GameState _currentState;
    private readonly Dictionary<string, Action<GameState>> _onEnter = new();
    private readonly Dictionary<string, Action<GameState>> _onExit = new();
    private object? _payload;

    public GameState CurrentState => _currentState;

    public GameStateMachine(IEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _currentState = GameState.Menu;
    }

    public void Run()
    {
        // One-step execution — the actual loop runs in GameLoop, not here.
        // Do NOT run an infinite loop here — it blocks the OpenTK event thread.
        switch (_currentState)
        {
            case GameState.Menu:
                // Menu handling — stubbed
                break;
            case GameState.Game:
                // Game running — GameLoop handles this
                break;
            case GameState.Pause:
                // Pause handling — stubbed
                break;
            case GameState.GameOver:
                // Game over — stubbed
                break;
        }
    }

    public void TransitionTo(GameState newState, object? payload = null)
    {
        _payload = payload;

        // Exit current state
        var oldKey = _currentState.ToString();
        if (_onExit.TryGetValue(oldKey, out var onExit))
            onExit(_currentState);

        _currentState = newState;

        // Enter new state
        var newKey = newState.ToString();
        if (_onEnter.TryGetValue(newKey, out var onEnter))
            onEnter(newState);

        _dispatcher.Dispatch(new StateTransitionEvent(_currentState));
    }

    public void TogglePause()
    {
        _currentState = _currentState switch
        {
            GameState.Game => GameState.Pause,
            GameState.Pause => GameState.Game,
            _ => _currentState
        };
    }

    public void RegisterOnEnter(string stateName, Action<GameState> handler)
    {
        _onEnter[stateName] = handler;
    }

    public void RegisterOnExit(string stateName, Action<GameState> handler)
    {
        _onExit[stateName] = handler;
    }

    public void Dispose() { }
}

// Event type for state transitions
public record StateTransitionEvent(GameState State) : IEvent;
