using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Djurspel.Core;
using Djurspel.Graphics;

namespace Djurspel.Gameplay;

/// <summary>
/// Concrete implementation of IInputManager — translates window keyboard/mouse input into queued gameplay events.
/// </summary>
public class InputManager : IInputManager
{
    private readonly IGameWindow _window;
    private readonly IEventDispatcher _dispatcher;
    private readonly List<Vector2> _moveQueues = new();
    private readonly List<Vector2> _attackQueues = new();
    private readonly List<int> _abilityQueues = new();
    private readonly Dictionary<int, MoralAlignment> _moralQueues = new();

    public InputManager(IGameWindow window, IEventDispatcher dispatcher)
    {
        _window = window;
        _dispatcher = dispatcher;
    }

    public void QueueMove(Vector2 screenTarget)
    {
        _moveQueues.Add(screenTarget);
    }

    public void QueueAttack(Vector2 screenTarget)
    {
        _attackQueues.Add(screenTarget);
    }

    public void QueueAbility(int abilityId)
    {
        _abilityQueues.Add(abilityId);
    }

    public void QueueMoralChoice(MoralAlignment choice, int companionId)
    {
        _moralQueues[companionId] = choice;
    }

    public void ClearQueues()
    {
        _moveQueues.Clear();
        _attackQueues.Clear();
        _abilityQueues.Clear();
        _moralQueues.Clear();
    }

    /// <summary>Process queued input and dispatch events. Call every frame.</summary>
    public void ProcessFrame()
    {
        // Process keyboard input for movement (WASD + right-click)
        if (_window.IsKeyDown((int)Keys.RightControl))
        {
            Vector2 mousePos = _window.MousePosition;
            QueueMove(mousePos);
            _dispatcher.Dispatch(new MoveQueuedEvent(1, new Core.Vec2(mousePos.X, mousePos.Y)));
        }

        // Left click to attack
        if (_window.IsMouseButtonPressed(0))
        {
            Vector2 mousePos = _window.MousePosition;
            QueueAttack(mousePos);
            _dispatcher.Dispatch(new AttackQueuedEvent(1, new Core.Vec2(mousePos.X, mousePos.Y)));
        }

        // Press E to use ability 1
        if (_window.IsKeyDown((int)Keys.E))
        {
            QueueAbility(1);
            _dispatcher.Dispatch(new AbilityQueuedEvent(1, 1));
        }

        ClearQueues();
    }

    public void Dispose()
    {
        _moveQueues.Clear();
        _attackQueues.Clear();
        _abilityQueues.Clear();
        _moralQueues.Clear();
    }
}
