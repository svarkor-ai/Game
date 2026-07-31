using OpenTK.Mathematics;
using Djurspel.Core;
using System;

namespace Djurspel.Gameplay;

/// <summary>
/// UI Manager för ARPG — hanterar health bar, inventory overlay, och andra UI-element.
/// Renderas som 2D-overlay över spelet.
/// </summary>
public class UIManager
{
    public class HealthBar
    {
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public Vector4 Color { get; set; } = Vector4.One;
        public Vector4 BackgroundColor { get; set; } = new Vector4(0.2f, 0.2f, 0.2f, 0.8f);
        public Vector4 ForegroundColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
    }

    public class InventorySlot
    {
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public string ItemName { get; set; } = "";
        public int Count { get; set; }
        public Vector4 Color { get; set; } = new Vector4(0.3f, 0.3f, 0.3f, 0.8f);
        public bool IsSelected { get; set; }
    }

    private readonly List<HealthBar> _healthBars = new();
    private readonly List<InventorySlot> _inventorySlots = new();
    private Vector2 _screenSize = new(1280, 720);
    private bool _inventoryOpen = false;

    public UIManager(Vector2 screenSize)
    {
        _screenSize = screenSize;
        SetupDefaultUI();
    }

    private void SetupDefaultUI()
    {
        // Player health bar at bottom center
        _healthBars.Add(new HealthBar
        {
            Position = new Vector2(_screenSize.X / 2 - 100, _screenSize.Y - 50),
            Size = new Vector2(200, 20),
            CurrentHealth = 100,
            MaxHealth = 100
        });

        // Inventory slots at bottom right
        for (int i = 0; i < 8; i++)
        {
            _inventorySlots.Add(new InventorySlot
            {
                Position = new Vector2(_screenSize.X - 220 + (i % 4) * 50, _screenSize.Y - 100 + (i / 4) * 50),
                Size = new Vector2(40, 40)
            });
        }
    }

    /// <summary>
    /// Uppdaterar UI state.
    /// </summary>
    public void Update(float frameTime, bool inventoryKeyJustPressed)
    {
        if (inventoryKeyJustPressed)
        {
            _inventoryOpen = !_inventoryOpen;
        }
    }

    /// <summary>
    /// Uppdaterar health bar med given health values.
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (_healthBars.Count > 0)
        {
            _healthBars[0].CurrentHealth = currentHealth;
            _healthBars[0].MaxHealth = maxHealth;
        }
    }

    /// <summary>
    /// Lägg till en health bar för en entity.
    /// </summary>
    public void AddHealthBar(HealthBar healthBar)
    {
        _healthBars.Add(healthBar);
    }

    /// <summary>
    /// Hämtar inventory state.
    /// </summary>
    public bool IsInventoryOpen => _inventoryOpen;
    public IReadOnlyList<InventorySlot> GetInventorySlots() => _inventorySlots.AsReadOnly();

    /// <summary>
    /// Hämtar player health bar.
    /// </summary>
    public HealthBar? GetPlayerHealthBar() => _healthBars.Count > 0 ? _healthBars[0] : null;
}
