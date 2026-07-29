using System;
using System.Collections.Generic;
using System.Diagnostics;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.Entities.Components;
using Djurspel.Gameplay;
using Djurspel.Graphics;
using Djurspel.World;

namespace Djurspel.Game;

/// <summary>
/// Concrete game loop — fixed timestep update with interpolation, render, frame pacing.
/// This runs on the GameWindow thread and drives all subsystems.
/// </summary>
public class GameLoop : IGameLoop
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IRenderer _renderer;
    private readonly IWorld _world;
    private readonly IEntityRegistry _registry;
    private readonly IInputManager _inputManager;
    private readonly ICombatManager _combatManager;
    private readonly IAIManager _aiManager;
    private readonly ISceneManager _sceneManager;
    private readonly IGameStateMachine _stateMachine;

    private double _fixedTimestep = 1.0 / 60.0;
    private readonly List<(Action<double> Update, string Name)> _updateHandlers = new();
    private readonly List<(Action<double> Render, string Name)> _renderHandlers = new();
    private readonly Stopwatch _timer = new();
    private double _accumulator = 0;
    private bool _running;
    private bool _stopped;
    private DateTime _lastFrameTime;

    public GameLoop(
        IEventDispatcher dispatcher,
        IRenderer renderer,
        IWorld world,
        IEntityRegistry registry,
        IInputManager inputManager,
        ICombatManager combatManager,
        IAIManager aiManager,
        ISceneManager sceneManager,
        IGameStateMachine gameStateMachine)
    {
        _dispatcher = dispatcher;
        _renderer = renderer;
        _world = world;
        _registry = registry;
        _inputManager = inputManager;
        _combatManager = combatManager;
        _aiManager = aiManager;
        _sceneManager = sceneManager;
        _stateMachine = gameStateMachine;

        // Register internal update handlers
        RegisterUpdate(UpdateFixed, "GameLoop");
        RegisterUpdate(UpdateGameplay, "Gameplay");
        RegisterRender(RenderScene, "Rendering");
    }

    public void SetFixedTimestep(double fps)
    {
        _fixedTimestep = 1.0 / fps;
    }

    public void RegisterUpdate(Action<double> update, string name)
    {
        _updateHandlers.Add((update, name));
    }

    public void RegisterRender(Action<double> render, string name)
    {
        _renderHandlers.Add((render, name));
    }

    public void Start()
    {
        _running = true;
        _stopped = false;
        _accumulator = 0;
        _lastFrameTime = DateTime.UtcNow;
        _timer.Restart();
    }

    public void Stop()
    {
        _stopped = true;
    }

    /// <summary>Run one frame — call from GameWindow's render loop.</summary>
    public void Frame()
    {
        if (_stopped || _running) return;

        var now = DateTime.UtcNow;
        var delta = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        // Accumulate and run fixed timestep updates
        _accumulator += delta;
        while (_accumulator >= _fixedTimestep)
        {
            foreach (var (update, name) in _updateHandlers)
            {
                update(_fixedTimestep);
            }
            _accumulator -= _fixedTimestep;
        }

        // Render at current time
        var interpolation = _accumulator / _fixedTimestep;
        foreach (var (render, name) in _renderHandlers)
        {
            render(interpolation);
        }
    }

    /// <summary>Process one fixed-timestep update frame. Call from GameWindow's update loop.</summary>
    public void UpdateFrame()
    {
        if (_stopped) return;

        // Accumulate and run fixed timestep updates
        var now = DateTime.UtcNow;
        var delta = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        _accumulator += delta;
        while (_accumulator >= _fixedTimestep)
        {
            foreach (var (update, name) in _updateHandlers)
            {
                update(_fixedTimestep);
            }
            _accumulator -= _fixedTimestep;
        }

        // Render at current time
        var interpolation = _accumulator / _fixedTimestep;
        foreach (var (render, name) in _renderHandlers)
        {
            render(interpolation);
        }
    }

    // ---- Internal update handlers ----

    private void UpdateFixed(double dt)
    {
        // Update state machine
        _stateMachine.Run();

        // Process entity deaths (cleanup)
        ProcessDeaths();

        // Process input queue events
        ProcessInputEvents();

        // Dispatch AI update event
        _dispatcher.Dispatch(new Gameplay.AIUpdateEvent((float)dt));
    }

    private void UpdateGameplay(double dt)
    {
        // Update combat (cooldowns, etc.)
        _combatManager.Update((float)dt);

        // Update AI
        _aiManager.Update((float)dt);
    }

    private void RenderScene(double interpolation)
    {
        _renderer.BeginScene();

        // Render world tiles
        if (_world != null)
        {
            foreach (var region in _world.GetVisibleTiles())
            {
                _renderer.DrawTileMap(_world, (object)region, (float)interpolation);
            }
        }

        // Render entities
        foreach (var entity in _registry.Query<RenderComponent>())
        {
            var render = entity.GetComponent<RenderComponent>();
            if (render != null && render.Visible)
            {
                _renderer.DrawEntity(entity, (float)interpolation);
            }
        }

        _renderer.EndScene();
    }

    private void ProcessDeaths()
    {
        var deadIds = _registry.ProcessDeaths();
        foreach (var id in deadIds)
        {
            // Entities are removed from registry in ProcessDeaths
        }
    }

    private void ProcessInputEvents()
    {
        // Stub — input events are processed through the IInputManager queues
        _inputManager.ProcessFrame();
    }

    public void Dispose()
    {
        _running = false;
        _stopped = true;
        _updateHandlers.Clear();
        _renderHandlers.Clear();
    }
}
