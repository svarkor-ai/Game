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
        private bool _disposed;

        public ARPGGameEngine()
        {
            // Initialize shader manager first
            _shaderManager = new ShaderManager();

            // Create the ARPG bootstrapper
            _bootstrapper = new ARPGGameBootstrapper();
        }

        public void Run()
        {
            // Create game window with the shader manager
            _gameWindow = new GameWindow(
                renderer: null!,  // Will be set after OpenGL context is created
                shaderManager: _shaderManager!,
                camera: null!,    // Will be set after bootstrapper initializes
                1280, 720);

            // Initialize the ARPG bootstrapper after window exists
            _bootstrapper!.Initialize(_gameWindow, null);

            // Wire up the update callback to use the ARPG bootstrapper
            _gameWindow.SetUpdateFrameCallback(OnUpdateFrame);
        }

        private void OnUpdateFrame(double deltaTime)
        {
            if (_bootstrapper == null || _gameWindow == null || _shaderManager == null)
                return;

            // Update ARPG game logic
            _bootstrapper.Update((float)deltaTime);

            // Render using the ARPG renderer
            _bootstrapper.Render(null!, _shaderManager);
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
            using var game = new ARPGGameEngine();
            game.Run();

            Console.Error.WriteLine("[Program] ARPG Game engine started. Press Enter to stop.");
            Console.ReadLine();

            game.Dispose();
        }
    }
}