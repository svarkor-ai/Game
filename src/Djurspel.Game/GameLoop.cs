using Djurspel.Core;
using Djurspel.Gameplay;
using Djurspel.Graphics;

namespace Djurspel.Game;

/// <summary>
/// Simple game loop that drives input processing and rendering with fixed timestep.
/// This is the actual game loop — no ECS, no state machine, just input→update→render.
/// </summary>
public class GameLoop : IGameLoop
{
    private readonly IInputManager _input;
    private readonly IRenderer _renderer;
    private readonly IShaderManager _shaderManager;
    private readonly Action<float> _update;
    private readonly Action<IRenderer, IShaderManager> _render;

    private double _fixedTimestep = 1.0 / 60.0;
    private double _accumulator = 0;
    private DateTime _lastFrameTime;
    private bool _running;
    private bool _stopped;

    public GameLoop(
        IInputManager input,
        IRenderer renderer,
        IShaderManager shaderManager,
        Action<float> update,
        Action<IRenderer, IShaderManager> render)
    {
        _input = input;
        _renderer = renderer;
        _shaderManager = shaderManager;
        _update = update;
        _render = render;
        _lastFrameTime = DateTime.UtcNow;
    }

    public void SetFixedTimestep(double fps)
    {
        _fixedTimestep = 1.0 / fps;
    }

    public void RegisterUpdate(System.Action<double> update, string name) { /* no-op */ }
    public void RegisterRender(System.Action<double> render, string name) { /* no-op */ }

    public void Start()
    {
        _running = true;
        _stopped = false;
        _accumulator = 0;
        _lastFrameTime = DateTime.UtcNow;
    }

    public void Stop()
    {
        _stopped = true;
    }

    public void Frame()
    {
        if (_stopped || !_running) return;

        var now = DateTime.UtcNow;
        var delta = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        // Clamp delta to avoid spiral of death
        if (delta > 0.25) delta = 0.25;

        _accumulator += delta;
        while (_accumulator >= _fixedTimestep)
        {
            UpdateFrame();
            _accumulator -= _fixedTimestep;
        }

        double interpolation = _accumulator / _fixedTimestep;
        RenderFrame((float)interpolation);
    }

    public void Dispose()
    {
        _running = false;
        _stopped = true;
    }

    private void UpdateFrame()
    {
        _input.ProcessFrame();
        _update(0.016f); // dt handled by fixed timestep
    }

    private void RenderFrame(float interpolation)
    {
        _render(_renderer, _shaderManager);
    }
}
