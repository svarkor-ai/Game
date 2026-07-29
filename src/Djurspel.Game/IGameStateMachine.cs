namespace Djurspel.Game;

/// <summary>Central game state machine.</summary>
public interface IGameStateMachine : IDisposable
{
    GameState CurrentState { get; }
    void Run();
    void TransitionTo(GameState newState, object? payload = null);
    void TogglePause();
}
