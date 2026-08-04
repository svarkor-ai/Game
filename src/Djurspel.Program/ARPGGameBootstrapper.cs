using Djurspel.Core;
using Djurspel.Gameplay;
using Djurspel.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Linq;

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
    private InventorySystem? _inventorySystem;
    private QuestSystem? _questSystem;
    private Vector2 _playerPosition = new(0, 0);
    private int _playerHealth = 100;
    private int _playerMaxHealth = 100;
    private int _playerGold = 0;
    private bool _gameInitialized = false;
    private IGameWindow? _window;
    private Matrix4? _projMatrix;
    private Matrix4? _viewMatrix;

    public void Initialize(IGameWindow window, IEventDispatcher? dispatcher)
    {
        _window = window;
        
        // Create camera first
        _camera = new TopDownCamera
        {
            WindowWidth = window?.Width ?? 1280,
            WindowHeight = window?.Height ?? 720
        };
        
        // Initialize camera to get initial matrices
        _camera.SetTarget(_playerPosition);
        _camera.Update(0.016f);
        
        // Get projection and view matrices for SpriteBatchRenderer
        _projMatrix = _camera!.GetProjectionMatrix();
        _viewMatrix = _camera!.GetViewMatrix();
        
        // Create SpriteBatchRenderer with the matrices
        _spriteRenderer = new SpriteBatchRenderer(_projMatrix.Value, _viewMatrix.Value);
        
        _inputManager = new ARPGInputManager(window!, dispatcher);
        _uiManager = new UIManager(new Vector2(
            window?.Width ?? 1280,
            window?.Height ?? 720
        ));
        
        // Create inventory and quest systems
        _inventorySystem = new InventorySystem();
        _questSystem = new QuestSystem();
        
        // Create enemies
        Random random = new(42); // Fixed seed for consistency
        _enemies = new EnemyAI[8]; // More enemies!
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
        Console.Error.WriteLine("[ARPG] Game bootstrapped with " + _enemies.Length + " enemies, inventory and quests!");
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
            int enemyDamage = enemy.Update(frameTime, _playerPosition);
            
            // Apply enemy damage to player
            if (enemyDamage > 0)
            {
                _playerHealth = Math.Max(0, _playerHealth - enemyDamage);
                Console.Error.WriteLine($"[Player] Took {enemyDamage} damage! HP: {_playerHealth}/{_playerMaxHealth}");
            }
            
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
                        _questSystem!.TrackKill();
                        
                        // Give gold reward
                        _playerGold += 10;
                        _inventorySystem!.AddItem(new InventorySystem.Item("Gold", "💰", 999, 0, 0, false));
                        
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
        
        // Update loot system and handle pickup
        _lootSystem!.Update(frameTime, _playerPosition, 2.0f);
        
        // Handle interact (E-key) for loot pickup
        if (_inputManager.InteractPressed && _inventorySystem != null)
        {
            foreach (var loot in _lootSystem!.GetItems())
            {
                float dist = Vector2.Distance(_playerPosition, loot.Position);
                if (dist < 3.0f && !loot.IsCollected)
                {
                    loot.IsCollected = true;
                    
                    // Add to inventory based on type
                    if (loot.ItemType == "gold")
                    {
                        _playerGold += loot.Value;
                        _inventorySystem.AddItem(new InventorySystem.Item("Gold", "💰", 999, 0, 0, false));
                    }
                    else if (loot.ItemType == "health_potion")
                    {
                        _inventorySystem.AddItem(new InventorySystem.Item("Health Potion", "🧪", 5, 0, 50, false));
                    }
                    else if (loot.ItemType == "weapon")
                    {
                        _inventorySystem.AddItem(new InventorySystem.Item("Weapon", "⚔️", 1, loot.Value, 0, true));
                    }
                    else if (loot.ItemType == "armor")
                    {
                        _inventorySystem.AddItem(new InventorySystem.Item("Armor", "🛡️", 1, 0, loot.Value, true));
                    }
                    break; // Only pickup one at a time
                }
            }
        }
        
        // Update UI
        _uiManager!.Update(frameTime, _inputManager.InventoryToggled);
        _uiManager.UpdateHealthBar(_playerHealth, _playerMaxHealth);
        
        // Update player health (simplified)
        if (_playerHealth > 0 && _playerHealth < _playerMaxHealth)
        {
            // Regen health slowly
            _playerHealth = Math.Min(_playerMaxHealth, _playerHealth + (int)(5.0f * frameTime));
        }
        else if (_playerHealth <= 0)
        {
            // Game over - respawn at full health
            _playerHealth = _playerMaxHealth;
            _playerPosition = new Vector2(0, 0);
            Console.Error.WriteLine("[Player] Respawned after death!");
        }
        
        // Use updated camera matrices — update renderer with fresh matrices
        if (_camera != null && _spriteRenderer != null)
        {
            _projMatrix = _camera.GetProjectionMatrix();
            _viewMatrix = _camera.GetViewMatrix();
            _spriteRenderer.SetMatrices(_projMatrix.Value, _viewMatrix.Value);
        }
    }

    public void Render(IRenderer renderer, IShaderManager shaderManager)
    {
        if (!_gameInitialized || _camera == null || _spriteRenderer == null || _uiManager == null || _enemies == null)
            return;
        
        // Clear screen
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f); // Dark background
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Use updated camera matrices
        _projMatrix = _camera!.GetProjectionMatrix();
        _viewMatrix = _camera!.GetViewMatrix();
        
        // Begin sprite batch (matrices are already set in constructor)
        _spriteRenderer!.BeginBatch();
        
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
            // Draw enemy as a circle (approximated with quad)
            _spriteRenderer.DrawQuad(enemy.Position, new Vector2(0.5f, 0.5f), enemy.GetColor());
            
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
        
        // DEBUG: Force a bright red square at origin to verify rendering works
        _spriteRenderer.DrawQuad(new Vector2(0f, 0f), new Vector2(2f, 2f), new Vector4(1f, 0f, 0f, 1f));
        
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
        if (_uiManager.IsInventoryOpen && _inventorySystem != null)
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
        
        // Draw quest info
        DrawQuestInfo();
        
        // Draw controls info
        DrawControlsInfo();
        
        // Draw gold
        DrawGold();
    }

    private void DrawQuestInfo()
    {
        if (_questSystem == null || _spriteRenderer == null) return;
        
        float yPos = 10;
        foreach (var quest in _questSystem.GetProgress())
        {
            string questText = quest.Completed ? $"✅ {quest.Title} (OK!)" : $"🎯 {quest.Title} ({quest.CurrentKills}/{quest.RequiredKills})";
            
            // Draw background for quest
            _spriteRenderer.DrawQuad(new Vector2(10, yPos), new Vector2(150, 15), new Vector4(0.0f, 0.0f, 0.0f, 0.7f));
            
            yPos += 20;
        }
    }

    private void DrawControlsInfo()
    {
        if (_spriteRenderer == null) return;
        
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
            _spriteRenderer.DrawQuad(new Vector2(10, yPos), new Vector2(120, 12), new Vector4(0.0f, 0.0f, 0.0f, 0.5f));
            yPos += 15;
        }
    }

    private void DrawGold()
    {
        if (_spriteRenderer == null) return;
        
        // Draw gold counter
        string goldText = $"💰 {_playerGold} Gold";
        _spriteRenderer.DrawQuad(new Vector2(10, 200), new Vector2(80, 15), new Vector4(0.0f, 0.0f, 0.0f, 0.5f));
    }

    public Vector2 GetPlayerPosition() => _playerPosition;
    public int GetPlayerHealth() => _playerHealth;
    public int GetPlayerMaxHealth() => _playerMaxHealth;
    public int GetPlayerGold() => _playerGold;
}
