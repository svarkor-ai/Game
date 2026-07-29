using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.Entities.Components;
using Djurspel.Graphics;
using Djurspel.World;
using Djurspel.Gameplay;
using Djurspel.Game;
using OpenTK.Mathematics;

namespace Djurspel.Program
{
    /// <summary>
    /// Game engine — initializes all modules and starts the game loop.
    /// </summary>
    public class GameEngine : IDisposable
    {
        private readonly IEventDispatcher _dispatcher;
        private readonly IAssetManager _assetManager;
        private readonly IGameWindow _window;
        private readonly GameWindow _gameWindow;
        private readonly IRenderer _renderer;
        private readonly IShaderManager _shaderManager;
        private readonly ICamera _camera;
        private readonly IWorld _world;
        private readonly IEntityRegistry _registry;
        private readonly IInputManager _input;
        private readonly ICombatManager _combat;
        private readonly IAIManager _ai;
        private readonly IGameStateMachine _stateMachine;
        private readonly ISceneManager _scene;
        private readonly IGameLoop _loop;
        private bool _disposed;

        public GameEngine()
        {
            // 1. Core
            _dispatcher = EventDispatcher.Instance;
            _assetManager = AssetManager.Instance;

              // 2. Graphics — GameWindow först (skapar OpenGL-kontext), sedan Renderer/ShaderManager/Camera
            _camera = new IsometricCamera();
            _gameWindow = new GameWindow(renderer: default!, shaderManager: default!, camera: _camera, 1280, 720);
            _window = _gameWindow;
            _renderer = new Renderer();
            _renderer.Initialize();
            _shaderManager = new ShaderManager();
            // Sätt renderer och shader på fönstret — nu när OpenGL-kontext finns
            _gameWindow.SetRendererAndShaderManager(_renderer, _shaderManager);

            // 3. World
            _world = WorldFactory.CreateFromPrimitive(64, 64, 1, TileType.Ground);

            // 4. Entities
            _registry = new EntityRegistry();
            var player = _registry.Create();
            player.Name = "Player";
            player.AddComponent(new TransformComponent { X = 0, Y = 0, Z = 0 });
            player.AddComponent(new HealthComponent { Current = 100, Max = 100 });
            player.AddComponent(new CombatComponent { AttackDamage = 10, AttackRange = 2f, AttackCooldown = 1f });
            player.AddComponent(new MovementComponent { Speed = 3f });
            player.AddComponent(new AIComponent { Behavior = AIBehavior.Idle });
            player.AddComponent(new RenderComponent { Visible = true, SpriteName = "player" });

            // 5. Gameplay
            _input = new InputManager(_window, _dispatcher);
            _combat = new CombatManager(_registry, _dispatcher);
            _ai = new AIManager(_registry, _world, _dispatcher);

            // 6. Game state & loop
            _stateMachine = new GameStateMachine(_dispatcher);
            _scene = new SceneManager();
            _loop = new GameLoop(
                _dispatcher,
                _renderer,
                _world,
                _registry,
                _input,
                _combat,
                _ai,
                _scene,
                _stateMachine);
        }

        public void Run()
        {
            _loop.Start();
        }

        public void Stop()
        {
            _loop.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _loop?.Dispose();
            _input?.Dispose();
            _combat?.Dispose();
            _ai?.Dispose();
            _stateMachine?.Dispose();
            _renderer?.Dispose();
            _registry?.Dispose();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using var game = new GameEngine();
            game.Run();
            System.Console.WriteLine("Game engine started. Press Enter to stop.");
            System.Console.ReadLine();
            game.Stop();
        }
    }
}
