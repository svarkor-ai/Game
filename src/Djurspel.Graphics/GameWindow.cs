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
    private readonly GameInput _input;
    private readonly GameScreenshot _screenshot;
    private readonly Dictionary<string, object> _dataStore = new();
    private float _frameTime = 0;

    public GameWindow(IRenderer renderer, IShaderManager shaderManager, ICamera camera, int width = 1280, int height = 720)
          : base(new OTK.GameWindowSettings(), new OTK.NativeWindowSettings
          {
              Title = "Djurspel",
              ClientSize = new Vector2i(width, height),
              StartVisible = true,
          })
     {
         _renderer = renderer;
         _shaderManager = shaderManager;
         _camera = camera;
         _updateFrameCallback = _ => { }; // default no-op
         _input = new GameInput(camera);
         _screenshot = new GameScreenshot(this);
     }

    /// <summary>Set path for headless auto-screenshot (saved on frame 10).</summary>
    public void SetHeadlessScreenshotPath(string path)
    {
        _screenshot.SetScreenshotPath(path);
    }

    /// <summary>
    /// Set renderer and shader manager after window creation.
    /// Called when OpenGL context is already active.
    /// </summary>
    public void SetRendererAndShaderManager(IRenderer renderer, IShaderManager shaderManager)
    {
        _renderer = renderer;
        _shaderManager = shaderManager;
        renderer.SetShaderManager(shaderManager);
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
        Console.Error.WriteLine("[OnLoad] Game loaded — initializing OpenGL...");
        
        _renderer.Initialize();
        
        Console.Error.WriteLine("[OnLoad] OpenGL initialized, GL context ready.");
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        _input.ProcessMovement(e.Time);
        Update();
        if (_updateFrameCallback != null)
            _updateFrameCallback(e.Time);
        else
            Console.Error.WriteLine("[GameWindow.OnUpdateFrame] WARNING: _updateFrameCallback is NULL!");
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        _screenshot.OnRenderFrame(e);
    }

    public void Render()
    {
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        OSG.GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        _input.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        _input.OnKeyUp(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        _input.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        _input.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        _input.OnMouseMove(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _input.OnMouseWheel(e);
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

    bool IGameWindow.IsKeyDown(int key) => _input.IsKeyDown(key);
    bool IGameWindow.IsMouseButtonPressed(int button) => _input.IsMouseButtonPressed(button);

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
        float moveSpeed = 5f;
        if (_input.IsKeyDown((int)Keys.W))
            cameraPos.Y += moveSpeed;
        if (_input.IsKeyDown((int)Keys.S))
            cameraPos.Y -= moveSpeed;
        if (_input.IsKeyDown((int)Keys.A))
            cameraPos.X -= moveSpeed;
        if (_input.IsKeyDown((int)Keys.D))
            cameraPos.X += moveSpeed;

        _camera.Position = cameraPos;
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
}