using Djurspel.Core;
using Djurspel.Graphics;
using Djurspel.Game;
using OpenTK.Mathematics;

namespace Djurspel.Program
{
    public class ARPGGameEngine : IDisposable
    {
        private ARPGGameBootstrapper? _bootstrapper;
        private GameWindow? _gameWindow;
        private ShaderManager? _shaderManager;
        private Renderer? _renderer;
        private TopDownCamera? _camera;
        private bool _disposed;
        private string? _screenshotPath;

        public ARPGGameEngine()
        {
            // Initialize shader manager first
            _shaderManager = new ShaderManager();
            _renderer = new Renderer(1280, 720);
            _camera = new TopDownCamera();
            _bootstrapper = new ARPGGameBootstrapper();
        }

        public string[]? Args { get; set; }

        public void SetScreenshotPath(string path)
        {
            _screenshotPath = path;
        }

        public void Run()
        {
            // Create game window with the shader manager and renderer
            _gameWindow = new GameWindow(
                renderer: _renderer!,
                shaderManager: _shaderManager!,
                camera: _camera!,
                1280, 720);

            // Wire up renderer and shader manager after OpenGL context is created
            _gameWindow.SetRendererAndShaderManager(_renderer!, _shaderManager!);

            // Initialize the ARPG bootstrapper after window exists
            _bootstrapper!.Initialize(_gameWindow, null);

            // Wire up the update callback to use the ARPG bootstrapper
            _gameWindow.SetUpdateFrameCallback(OnUpdateFrame);

            // Set screenshot path if provided
            if (_screenshotPath != null)
            {
                _gameWindow.SetHeadlessScreenshotPath(_screenshotPath);
            }

            // Start the game loop — this blocks until the window is closed
            Console.Error.WriteLine("[ARPGGameEngine] Starting game loop...");
            _gameWindow.Run();
            Console.Error.WriteLine("[ARPGGameEngine] Game loop finished.");
        }

        private void OnUpdateFrame(double deltaTime)
        {
            if (_bootstrapper == null || _gameWindow == null || _shaderManager == null)
                return;

            // Update ARPG game logic
            _bootstrapper.Update((float)deltaTime);

            // Render using the ARPG renderer
            // Create a dummy renderer for bootstrapper's Render method
            _bootstrapper.Render(_renderer!, _shaderManager);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gameWindow?.Close();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Parse --headless and --screenshot <path> flags
            bool headless = false;
            string? screenshotPath = null;
            foreach (var arg in args)
            {
                if (arg == "--headless") headless = true;
                if (arg == "--screenshot")
                {
                    // Find the next arg
                }
            }
            // Parse --screenshot <path>
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--screenshot" && i + 1 < args.Length)
                    screenshotPath = args[i + 1];
            }

            if (headless || screenshotPath != null)
            {
                // Headless mode: use HeadlessRenderer to render to BMP
                Console.Error.WriteLine("[Program] Starting headless mode...");
                
                var outputPath = screenshotPath ?? "/tmp/djurspel_headless.png";
                var headlessRenderer = new HeadlessRenderer(1280, 720, outputPath);
                
                // Simulate a simple render (just a colored rectangle)
                headlessRenderer.BeginBatch();
                for (int y = 0; y < 720; y++)
                {
                    for (int x = 0; x < 1280; x++)
                    {
                        int idx = y * 1280 * 4 + x * 4;
                        byte r = (byte)((x + y) % 256);
                        byte g = (byte)((x * y) % 256);
                        byte b = (byte)((x ^ y) % 256);
                        headlessRenderer._pixels[idx] = 255; // Alpha
                        headlessRenderer._pixels[idx + 1] = g; // G
                        headlessRenderer._pixels[idx + 2] = b; // B
                        headlessRenderer._pixels[idx + 3] = r; // R
                    }
                }
                headlessRenderer.EndBatch();
                headlessRenderer.SaveToBitmap();
                
                headlessRenderer.Dispose();
                
                Console.Error.WriteLine("[Program] Screenshot saved to " + outputPath);
            }
            else
            {
                // Normal interactive mode
                using var game = new ARPGGameEngine();
                game.Args = args;
                game.Run();
                Console.Error.WriteLine("[Program] ARPG Game engine stopped.");
            }
        }
    }
}