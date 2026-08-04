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
    private int _renderFrameCount = 0;
    private string? _headlessScreenshotPath = null;

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
     }

    /// <summary>Set path for headless auto-screenshot (saved on frame 10).</summary>
    public void SetHeadlessScreenshotPath(string path)
    {
        _headlessScreenshotPath = path;
    }

    /// <summary>
    /// Set renderer and shader manager after window creation.
    /// Called when OpenGL context is already active.
    /// </summary>
    public void SetRendererAndShaderManager(IRenderer renderer, IShaderManager shaderManager)
    {
        _renderer = renderer;
        _shaderManager = shaderManager;
        // Also set shader manager on the renderer so DrawEntity can use it
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
        
        // Initialize renderer (sets clear color, viewport, etc.)
        _renderer.Initialize();
        
        Console.Error.WriteLine("[OnLoad] OpenGL initialized, GL context ready.");
    }

      protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        Console.Error.WriteLine("[GameWindow.OnUpdateFrame] Called! _renderer=" + (_renderer == null ? "NULL" : "NOT_NULL") + " _callback=" + (_updateFrameCallback == null ? "NULL" : "NOT_NULL"));
        Update();
        // NOTE: Rendering is handled entirely by GameLoop.RenderScene (called from _updateFrameCallback).
        // Do NOT call Renderer.Render() here — it draws with _dummyWorld which is null!
        if (_updateFrameCallback != null)
            _updateFrameCallback(e.Time);
        else
            Console.Error.WriteLine("[GameWindow.OnUpdateFrame] WARNING: _updateFrameCallback is NULL!");
    }

   protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        // Only swap buffers — BeginScene() is called in OnUpdateFrame BEFORE rendering.
        // Do NOT call BeginScene() here or it will clear the frame we just drew!
        
        // Auto-screenshot on frame 10 in headless mode
        _renderFrameCount++;
        if (_renderFrameCount == 10 && _headlessScreenshotPath != null)
        {
            // Read the back buffer BEFORE swapping (where the frame was just drawn)
            // Force GPU synchronization — critical for llvmpipe/Xvfb
            OSG.GL.ReadBuffer(OSG.ReadBufferMode.Back);
            OSG.GL.Flush();
            OSG.GL.Finish();
            CaptureFramebufferToPng(_headlessScreenshotPath!);
            Console.Error.WriteLine("[GameWindow] Screenshot captured, now swapping...");
            SwapBuffers();
            Console.Error.WriteLine("[GameWindow] Screenshot saved to " + _headlessScreenshotPath);
            _headlessScreenshotPath = null;
            // Close the window after taking screenshot in headless mode
            Close();
        }
        else
        {
            SwapBuffers();
        }
    }

    public void Render()
    {
        // Legacy — does nothing now that rendering happens in OnUpdateFrame
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

    /// <summary>Take a single headless screenshot without a full game loop.</summary>
    public void TakeHeadlessScreenshot(string outputPath, int frames = 10)
    {
        SetHeadlessScreenshotPath(outputPath);
        
        // Manually run the frame loop — OnRenderFrame may not fire in Xvfb with Run()
        for (int i = 0; i < frames; i++)
        {
            // Call the actual OnUpdateFrame override (not base)
            Update();
            var updateCB = _updateFrameCallback;
            if (updateCB != null)
                updateCB(1.0 / 60.0);
            
            // Call OnRenderFrame logic directly (not just SwapBuffers via Render())
            // This is critical — OnRenderFrame has the ReadPixels logic at frame 10
            OnRenderFrame(new FrameEventArgs(1.0 / 60.0));
        }
        
        Console.Error.WriteLine("[GameWindow] Headless screenshot done, frames=" + frames);
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

    #region Framebuffer Screenshot (OpenGL ReadPixels → BMP)

    /// <summary>Read OpenGL back framebuffer and save as BMP (no external tools needed).</summary>
    private void CaptureFramebufferToPng(string outputPath)
    {
        try
        {
            int w = Size.X;
            int h = Size.Y;
            int rowSize = w * 3;
            int paddedRowSize = (rowSize + 3) & ~3; // row must be multiple of 4
            int imageSize = paddedRowSize * h;
            int fileSize = 54 + imageSize; // 54-byte BMP header + pixel data

            Console.Error.WriteLine($"[GameWindow] CaptureFramebufferToPng: {w}x{h}, imageSize={imageSize}, path={outputPath}");

            // Explicitly read from BACK buffer (where GL draws)
            OSG.GL.ReadBuffer(OSG.ReadBufferMode.Back);
            byte[] pixels = new byte[imageSize];
            OSG.GL.ReadPixels(0, 0, w, h, OSG.PixelFormat.Rgb, OSG.PixelType.UnsignedByte, pixels);
            
            // Also check for OpenGL errors
            OSG.GL.Finish();
            int err;
            List<string> errors = new();
            while ((err = (int)OSG.GL.GetError()) != 0)
            {
                errors.Add($"GL_ERROR: {err}");
            }
            if (errors.Count > 0)
                Console.Error.WriteLine("[GameWindow] GL errors during capture: " + string.Join(", ", errors));
            
            Console.Error.WriteLine($"[GameWindow] ReadPixels: read {pixels.Length} bytes, first 10: {string.Join(",", pixels.Take(10))}");

            // Analyze unique colors (sample every 1000th pixel for speed)
            var uniqueColors = new System.Collections.Generic.HashSet<string>();
            int sampledCount = 0;
            int zeroCount = 0;
            for (int i = 0; i < pixels.Length; i += 3)
            {
                int r = pixels[i], g = pixels[i + 1], b = pixels[i + 2];
                if (r == 0 && g == 0 && b == 0) zeroCount++;
                uniqueColors.Add($"{r},{g},{b}");
                sampledCount++;
            }
            Console.Error.WriteLine($"[GameWindow] ReadPixels analysis: sampled={sampledCount}, zeros={zeroCount}, unique_colors={uniqueColors.Count}");
            var topColors = uniqueColors.Take(20).ToList();
            Console.Error.WriteLine($"[GameWindow] Top colors: {string.Join(" | ", topColors)}");

            // Flip vertically (OpenGL stores bottom-to-top)
            byte[] flipRow = new byte[paddedRowSize];
            for (int y = 0; y < h / 2; y++)
            {
                int top = y * paddedRowSize;
                int bottom = (h - 1 - y) * paddedRowSize;
                Array.Copy(pixels, top, flipRow, 0, paddedRowSize);
                Array.Copy(pixels, bottom, pixels, top, paddedRowSize);
                Array.Copy(flipRow, 0, pixels, bottom, paddedRowSize);
            }

            // Pad rows (add zero bytes to make each row multiple of 4 bytes)
            byte[] paddedPixels = new byte[imageSize];
            for (int y = 0; y < h; y++)
            {
                Array.Copy(pixels, y * rowSize, paddedPixels, y * paddedRowSize, rowSize);
            }

            // Write BMP header
            Console.Error.WriteLine("[GameWindow] Writing BMP file...");
            using var fs = System.IO.File.Create(outputPath);
            using var bw = new System.IO.BinaryWriter(fs);

            // BM signature
            bw.Write((byte)'B');
            bw.Write((byte)'M');
            // File size
            bw.Write(fileSize);
            bw.Write((short)0); // reserved
            bw.Write((short)0); // reserved
            // Offset to pixel data
            bw.Write(54);
            // DIB header size (BITMAPINFOHEADER)
            bw.Write(40);
            // Width and height
            bw.Write(w);
            bw.Write(h);
            // Planes (1) and bits per pixel (24)
            bw.Write((short)1);
            bw.Write((short)24);
            // Compression (0 = none), image size, resolution
            bw.Write(0);
            bw.Write(imageSize);
            bw.Write(2835); // horizontal resolution (72 DPI)
            bw.Write(2835); // vertical resolution
            bw.Write(0);    // colors in table
            bw.Write(0);    // important colors

            Console.Error.WriteLine("[GameWindow] BMP header written. Writing pixels...");
            bw.Write(paddedPixels);
            bw.Flush();
            Console.Error.WriteLine("[GameWindow] BMP file written successfully: " + fileSize + " bytes");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[GameWindow] ERROR in CaptureFramebufferToPng: " + ex.ToString());
        }
    }

    #endregion
}
