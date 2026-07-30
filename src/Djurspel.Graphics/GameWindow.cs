using Djurspel.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OTK = OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OSG = OpenTK.Graphics.OpenGL;

namespace Djurspel.Graphics;

/// <summary>
/// Spelfönster som använder OpenTK 4 GameWindow.
/// Event-driven: använder events för indata, state-polling för rendering.
/// Implementerar IGameWindow-avgränsningsgränssnittet.
/// </summary>
public class GameWindow : OTK.GameWindow, IGameWindow
{
    private IRenderer _renderer;
    private IShaderManager _shaderManager;
    private ICamera _camera;
    private Action<double> _updateFrameCallback;
    private readonly Dictionary<string, object> _dataStore = new();
    private readonly HashSet<int> _pressedKeys = new();
    private readonly HashSet<int> _pressedButtons = new();
    private float _frameTime = 0;

    public GameWindow(IRenderer renderer, IShaderManager shaderManager, ICamera camera, int width = 1280, int height = 720)
        : base(new OTK.GameWindowSettings(), new OTK.NativeWindowSettings
        {
            Title = "Djurspel",
            ClientSize = new Vector2i(width, height),
            StartVisible = false,
        })
    {
        _renderer = renderer;
        _shaderManager = shaderManager;
        _camera = camera;
        _updateFrameCallback = _ => { }; // default no-op

        // OpenGL-initiering — måste ske efter att fönstret skapats (OpenGL-kontext)
        Context.MakeCurrent();
        InitializeGL();
    }

    /// <summary>
    /// Set renderer and shader manager after window creation.
    /// Called when OpenGL context is already active.
    /// </summary>
    public void SetRendererAndShaderManager(IRenderer renderer, IShaderManager shaderManager)
    {
        _renderer = renderer;
        _shaderManager = shaderManager;
    }

    /// <summary>
    /// Set the update-frame callback from GameLoop.
    /// Called by GameEngine to wire GameWindow → GameLoop.
    /// </summary>
    public void SetUpdateFrameCallback(Action<double> callback)
    {
        _updateFrameCallback = callback;
    }

    #region Event hanterare

    protected override void OnLoad()
    {
        base.OnLoad();
        Context.MakeCurrent();
    }

      protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        Update();
        _updateFrameCallback(e.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        Render();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        OSG.GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        _pressedKeys.Add((int)e.Key);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        _pressedKeys.Remove((int)e.Key);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.IsPressed)
            _pressedButtons.Add((int)e.Button);
        else
            _pressedButtons.Remove((int)e.Button);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (!e.IsPressed)
            _pressedButtons.Remove((int)e.Button);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        UpdateCameraPositionFromDelta(e.Delta);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _camera.Zoom -= e.OffsetY * 0.5f;
    }

    #endregion

    #region IGameWindow explicit implementation

    void IGameWindow.SwapBuffers() => SwapBuffers();
    void IGameWindow.SetTitle(string title) => Title = title;
    void IGameWindow.ProcessEvents() => ProcessEvents(1.0);
    void IGameWindow.Close() => Close();

    int IGameWindow.Width => Size.X;
    int IGameWindow.Height => Size.Y;
    bool IGameWindow.IsOpen => Exists && !IsExiting;
    bool IGameWindow.ShouldClose => IsExiting;

    Vector2 IGameWindow.MousePosition => MousePosition;

    bool IGameWindow.IsKeyDown(int key) => _pressedKeys.Contains(key);
    bool IGameWindow.IsMouseButtonPressed(int button) => _pressedButtons.Contains(button);

    #endregion

    #region Game loop och status

    public bool IsRunning => Exists && !IsExiting;

    public void RunGameLoop()
    {
        Run();
    }

    public void Update()
    {
        var cameraPos = _camera.Position;

        // Uppdatera kamerans position baserat på tangenttryckningar
        float moveSpeed = 5f;
        if (_pressedKeys.Contains((int)Keys.W))
            cameraPos.Y += moveSpeed;
        if (_pressedKeys.Contains((int)Keys.S))
            cameraPos.Y -= moveSpeed;
        if (_pressedKeys.Contains((int)Keys.A))
            cameraPos.X -= moveSpeed;
        if (_pressedKeys.Contains((int)Keys.D))
            cameraPos.X += moveSpeed;

        _camera.Position = cameraPos;
    }

    public void Render()
    {
        _renderer.Render(_camera, _shaderManager, _frameTime);
        SwapBuffers();
    }

    #endregion

    #region Data store

    public object GetOrCreateData(string key, Func<object> factory)
    {
        if (!_dataStore.TryGetValue(key, out var value))
        {
            value = factory();
            _dataStore[key] = value;
        }
        return value;
    }

    public void SetData(string key, object value)
    {
        _dataStore[key] = value;
    }

    public object? GetData(string key) => _dataStore.TryGetValue(key, out var v) ? v : null;

    #endregion

    #region OpenGL-initiering

    private void InitializeGL()
    {
        OSG.GL.Enable(OSG.EnableCap.DepthTest);
        OSG.GL.Enable(OSG.EnableCap.CullFace);
        OSG.GL.CullFace(OSG.CullFaceMode.Back);
        OSG.GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);
    }

    #endregion

    #region Kamerahjälp

    private void UpdateCameraPositionFromDelta(Vector2 delta)
    {
        var pos = _camera.Position;
        // Omvänd musrörelse för kamerastyrning
        pos.X -= delta.X * 0.5f;
        pos.Z -= delta.Y * 0.5f;
        _camera.Position = pos;
    }

    #endregion
}
