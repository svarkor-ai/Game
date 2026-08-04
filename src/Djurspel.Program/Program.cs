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

        public ARPGGameEngine()
        {
            // Initialize shader manager first
            _shaderManager = new ShaderManager();
            _renderer = new Renderer(1280, 720);
            _camera = new TopDownCamera();
            _bootstrapper = new ARPGGameBootstrapper();
        }

        public string[]? Args { get; set; }

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

           // Headless screenshot support
            if (Args != null && Args.Length > 0)
            {
                for (int i = 0; i < Args.Length; i++)
                {
                    if (Args[i] == "--screenshot" && i + 1 < Args.Length)
                    {
                        _gameWindow.SetHeadlessScreenshotPath(Args[i + 1]);
                        break;
                    }
                }
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
            bool headless = false;
            string screenshotPath = null;
            foreach (var arg in args)
            {
                if (arg == "--headless") headless = true;
                if (arg == "--screenshot" && screenshotPath == null)
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
                // Headless mode: create window, run few frames, take screenshot, exit
                Console.Error.WriteLine("[Program] Starting headless mode...");
                
                var shaderManager = new ShaderManager();
                var renderer = new Renderer(1280, 720);
                var camera = new TopDownCamera();
                var bootstrapper = new ARPGGameBootstrapper();
                
                var gameWindow = new GameWindow(
                    renderer: renderer,
                    shaderManager: shaderManager,
                    camera: camera,
                    1280, 720);
                
                gameWindow.SetRendererAndShaderManager(renderer, shaderManager);
                bootstrapper.Initialize(gameWindow, null);
                
                // Set screenshot path for auto-capture
                if (screenshotPath != null)
                    gameWindow.SetHeadlessScreenshotPath(screenshotPath);
                
                // Wire update callback
                Action<double> updateCallback = dt => {
                    bootstrapper.Update((float)dt);
                    bootstrapper.Render(renderer, shaderManager);
                };
                gameWindow.SetUpdateFrameCallback(updateCallback);
                
                // Take screenshot via manual frame loop
                gameWindow.TakeHeadlessScreenshot(screenshotPath ?? "/tmp/djurspel_headless.png", frames: 15);
                
                // Clean up
                gameWindow.Close();
                
                if (screenshotPath != null)
                    Console.Error.WriteLine("[Program] Screenshot saved to " + screenshotPath);
                else
                    Console.Error.WriteLine("[Program] Headless mode done (no screenshot path given).");
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