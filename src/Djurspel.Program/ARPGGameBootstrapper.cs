using Djurspel.Core;
using Djurspel.Graphics;
using Djurspel.Gameplay;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Djurspel.Game;

/// <summary>
/// ARPG Game Bootstrapper — sätter upp spelet med nya ARPG-komponenter
/// och konfigurerar dem för top-down 2D rendering.
/// </summary>
public class ARPGGameBootstrapper
{
    private TopDownCamera? _camera;
    private SpriteBatchRenderer? _spriteRenderer;
    private EnemyAI[]? _enemies;
    private LootDropSystem? _lootSystem;
    private UIManager? _uiManager;
    private ARPGInputManager? _inputManager;
    private Vector2 _playerPosition = new(0, 0);
    private int _playerHealth = 100;
    private int _playerMaxHealth = 100;
    private bool _gameInitialized = false;
    private IGameWindow? _window;

    public void Initialize(IGameWindow window, IEventDispatcher? dispatcher)
    {
        _window = window;
        
        // Create ARPG components
        _camera = new TopDownCamera
        {
            WindowWidth = window?.Width ?? 1280,
            WindowHeight = window?.Height ?? 720
        };
        
        _spriteRenderer = new SpriteBatchRenderer();
        _inputManager = new ARPGInputManager(window!, dispatcher);
        _uiManager = new UIManager(new Vector2(
            window?.Width ?? 1280,
            window?.Height ?? 720
        ));
        
        // Create enemies
        Random random = new(42); // Fixed seed for consistency
        _enemies = new EnemyAI[5];
        for (int i = 0; i < _enemies.Length; i++)
        {
            float angle = (float)(random.NextDouble() * 2.0 * Math.PI);
            float distance = 3.0f + (float)(random.NextDouble() * 5.0f);
            Vector2 spawnPos = new(
                MathF.Cos(angle) * distance,
                MathF.Sin(angle) * distance
            );
            _enemies[i] = new EnemyAI(spawnPos, random);
        }
        
        _lootSystem = new LootDropSystem();
        
        _gameInitialized = true;
        Console.Error.WriteLine("[ARPG] Game bootstrapped with " + _enemies.Length + " enemies");
    }

    public void Update(float frameTime)
    {
        if (!_gameInitialized || _camera == null || _inputManager == null || _enemies == null)
            return;
        
        // Update input
        _inputManager.Update(frameTime);
        
        // Update player position based on input
        Vector2 movement = _inputManager.GetNormalizedMovement();
        _playerPosition += movement * 5.0f * frameTime; // 5 units/sec
        
        // Update camera to follow player
        _camera.SetTarget(_playerPosition);
        _camera.Update(frameTime);
        
        // Update enemies
        foreach (var enemy in _enemies)
        {
            enemy.Update(frameTime, _playerPosition);
            
            // Check if player attacks enemy
            if (_inputManager.AttackPressed)
            {
                float dist = Vector2.Distance(_playerPosition, enemy.Position);
                if (dist < 2.0f) // Attack range
                {
                    enemy.TakeDamage(25);
                    
                    // Drop loot on death
                    if (enemy.IsDead)
                    {
                        _lootSystem!.DropLoot(enemy.Position, 3);
                        // Respawn enemy elsewhere
                        Random random = new();
                        float angle = (float)(random.NextDouble() * 2.0 * Math.PI);
                        float distance = 8.0f + (float)(random.NextDouble() * 5.0f);
                        enemy.Position = new Vector2(
                            _playerPosition.X + MathF.Cos(angle) * distance,
                            _playerPosition.Y + MathF.Sin(angle) * distance
                        );
                        enemy.Health = enemy.MaxHealth;
                        enemy.CurrentState = EnemyAI.State.Wander;
                    }
                }
            }
        }
        
        // Update loot system
        _lootSystem!.Update(frameTime, _playerPosition);
        
        // Update UI
        _uiManager!.Update(frameTime, _inputManager.InventoryToggled);
        _uiManager.UpdateHealthBar(_playerHealth, _playerMaxHealth);
        
        // Track player health (simplified)
        if (_playerHealth > 0 && _playerHealth < _playerMaxHealth)
        {
            // Regen health slowly
            _playerHealth = Math.Min(_playerMaxHealth, _playerHealth + (int)(5.0f * frameTime));
        }
    }

    public void Render(IRenderer renderer, IShaderManager shaderManager)
    {
        if (!_gameInitialized || _camera == null || _spriteRenderer == null || _uiManager == null || _enemies == null)
            return;
        
        // Clear screen
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f); // Dark background
        
        // Begin sprite batch
        _spriteRenderer.BeginBatch();
        
        // Set up shaders for 2D rendering
       shaderManager.Bind(shaderManager.Get("SpriteBatch") ?? new Djurspel.Graphics.ShaderProgram());
        var projMatrix = _camera!.GetProjectionMatrix();
        var viewMatrix = _camera!.GetViewMatrix();
        // Convert to float[] for SetMat4 — OpenTK Matrix4 is column-major
        float[] projFloats = new float[16];
        float[] viewFloats = new float[16];
        for (int row = 0; row < 4; row++)
        for (int col = 0; col < 4; col++)
        {
            projFloats[col * 4 + row] = projMatrix[row, col];
            viewFloats[col * 4 + row] = viewMatrix[row, col];
        }
        shaderManager.SetMat4("uProjection", projFloats);
        shaderManager.SetMat4("uView", viewFloats);
        
        // Draw floor tiles (simple grid)
        _spriteRenderer.DrawQuad(new Vector2(-20, -20), new Vector2(40, 40), new Vector4(0.2f, 0.2f, 0.25f, 1.0f));
        
        // Draw grid lines
        for (float x = -20; x <= 20; x += 2)
        {
            _spriteRenderer.DrawQuad(new Vector2(x, -20), new Vector2(0.02f, 40), new Vector4(0.3f, 0.3f, 0.35f, 1.0f));
        }
        for (float y = -20; y <= 20; y += 2)
        {
            _spriteRenderer.DrawQuad(new Vector2(-20, y), new Vector2(40, 0.02f), new Vector4(0.3f, 0.3f, 0.35f, 1.0f));
        }
        
        // Draw loot items
        foreach (var loot in _lootSystem!.GetItems())
        {
            _spriteRenderer.DrawQuad(loot.Position, new Vector2(0.3f, 0.3f), loot.Color);
        }
        
        // Draw enemies
        foreach (var enemy in _enemies)
        {
            Vector4 enemyColor = enemy.CurrentState switch
            {
                EnemyAI.State.Wander => new Vector4(0.8f, 0.2f, 0.2f, 1.0f), // Red
                EnemyAI.State.Chase => new Vector4(1.0f, 0.4f, 0.0f, 1.0f), // Orange
                EnemyAI.State.Attack => new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Bright red
                _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f) // Gray
            };
            
            // Draw enemy as a circle (approximated with quad)
            _spriteRenderer.DrawQuad(enemy.Position, new Vector2(0.5f, 0.5f), enemyColor);
            
            // Draw health bar above enemy
            float healthPercent = (float)enemy.Health / enemy.MaxHealth;
            Vector2 healthBarPos = new Vector2(
                enemy.Position.X - 0.3f,
                enemy.Position.Y + 0.4f
            );
            _spriteRenderer.DrawQuad(healthBarPos, new Vector2(0.6f, 0.05f), new Vector4(0.2f, 0.2f, 0.2f, 0.8f)); // Background
            _spriteRenderer.DrawQuad(healthBarPos, new Vector2(0.6f * healthPercent, 0.05f), new Vector4(0.0f, 1.0f, 0.0f, 0.8f)); // Health
        }
        
        // Draw player (blue circle)
        _spriteRenderer.DrawQuad(_playerPosition, new Vector2(0.4f, 0.4f), new Vector4(0.2f, 0.4f, 1.0f, 1.0f));
        
        // End sprite batch
        _spriteRenderer.EndBatch();
        
        // Draw UI overlay (health bar, inventory)
        DrawUI(renderer, shaderManager);
    }

    private void DrawUI(IRenderer renderer, IShaderManager shaderManager)
    {
        if (_uiManager == null) return;
        
        // Draw health bar
        var healthBar = _uiManager.GetPlayerHealthBar();
        if (healthBar != null)
        {
            float healthPercent = healthBar.CurrentHealth / healthBar.MaxHealth;
            
            // Background
            _spriteRenderer!.DrawQuad(healthBar.Position, healthBar.Size, healthBar.BackgroundColor);
            
            // Health fill
            Vector2 healthSize = new Vector2(healthBar.Size.X * healthPercent, healthBar.Size.Y);
            _spriteRenderer.DrawQuad(
                new Vector2(healthBar.Position.X, healthBar.Position.Y),
                healthSize,
                healthBar.ForegroundColor
            );
            
            // Health text (simplified — in real app would use text rendering)
            string healthText = $"{healthBar.CurrentHealth}/{healthBar.MaxHealth}";
            // Position text in center of health bar
            Vector2 textPos = new Vector2(
                healthBar.Position.X + healthBar.Size.X / 2 - 20,
                healthBar.Position.Y + healthBar.Size.Y / 2 - 5
            );
            // Draw a simple background for text
            _spriteRenderer.DrawQuad(textPos, new Vector2(40, 10), new Vector4(0.0f, 0.0f, 0.0f, 0.5f));
        }
        
        // Draw inventory if open
        if (_uiManager.IsInventoryOpen)
        {
            // Draw inventory background
            _spriteRenderer!.DrawQuad(new Vector2(100, 100), new Vector2(200, 150), new Vector4(0.1f, 0.1f, 0.2f, 0.9f));
            
            // Draw inventory slots
            foreach (var slot in _uiManager.GetInventorySlots())
            {
                Vector4 slotColor = slot.IsSelected 
                    ? new Vector4(0.4f, 0.4f, 0.8f, 0.8f) 
                    : slot.Color;
                _spriteRenderer.DrawQuad(slot.Position, slot.Size, slotColor);
            }
        }
        
        // Draw controls info
        DrawControlsInfo();
    }

    private void DrawControlsInfo()
    {
        // Simple controls overlay
        string[] controls = new string[]
        {
            "WASD/Arrows: Move",
            "Mouse: Aim",
            "Left Click: Attack",
            "E: Interact",
            "I: Inventory"
        };
        
        float yPos = 10;
        foreach (var control in controls)
        {
            // Draw a simple background for text
            _spriteRenderer!.DrawQuad(new Vector2(10, yPos), new Vector2(120, 12), new Vector4(0.0f, 0.0f, 0.0f, 0.5f));
            yPos += 15;
        }
    }

    public Vector2 GetPlayerPosition() => _playerPosition;
    public int GetPlayerHealth() => _playerHealth;
    public int GetPlayerMaxHealth() => _playerMaxHealth;
}