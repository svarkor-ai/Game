using OpenTK.Mathematics;
using Djurspel.Core;
using System;
using System.Collections.Generic;

namespace Djurspel.Gameplay;

/// <summary>
/// Loot system för ARPG — hanterar loot drops och pickup.
/// Simulerar enkel loot-mekanik med various item types.
/// </summary>
public class LootDropSystem
{
    public class LootItem
    {
        public Vector2 Position { get; set; }
        public string ItemType { get; set; } // "health_potion", "gold", "weapon", etc.
        public int Value { get; set; }
        public Vector2 Velocity { get; set; }
        public float Lifetime { get; set; }
        public bool IsCollected { get; set; }
        public Vector4 Color { get; set; } // For rendering
        
        public LootItem(Vector2 position, string itemType, int value, Random random)
        {
            Position = position;
            ItemType = itemType;
            Value = value;
            Lifetime = 30f; // Items despawn after 30 seconds
            
            // Random color based on type
            Color = GetColorForItemType(itemType);
            
            // Slight random velocity for "scatter" effect
            float angle = (float)(random.NextDouble() * 2.0 * Math.PI);
            float speed = 1.0f + (float)random.NextDouble();
            Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
        }
        
        private static Vector4 GetColorForItemType(string itemType)
        {
            return itemType switch
            {
                "health_potion" => new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Red
                "gold" => new Vector4(1.0f, 0.8f, 0.0f, 1.0f), // Yellow
                "weapon" => new Vector4(0.5f, 0.5f, 1.0f, 1.0f), // Blue
                "armor" => new Vector4(0.5f, 1.0f, 0.5f, 1.0f), // Green
                _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f) // White
            };
        }
    }

    private readonly List<LootItem> _lootItems = new();
    private readonly Random _random = new();

    /// <summary>
    /// Skapar loot drops från en dödad entity.
    /// </summary>
    public void DropLoot(Vector2 position, int count = 3)
    {
        for (int i = 0; i < count; i++)
        {
            string itemType = GetRandomItemType();
            int value = _random.Next(5, 50);
            _lootItems.Add(new LootItem(position, itemType, value, _random));
        }
    }

    /// <summary>
    /// Uppdaterar loot items och kollar pickup.
    /// </summary>
    public void Update(float frameTime, Vector2 playerPosition, float pickupRange = 2.0f)
    {
        for (int i = _lootItems.Count - 1; i >= 0; i--)
        {
            var item = _lootItems[i];
            
            // Apply velocity (items float around a bit)
            item.Position += item.Velocity * frameTime;
            item.Velocity *= 0.95f; // Damping
            
            // Update lifetime
            item.Lifetime -= frameTime;
            
            // Check pickup
            float distance = Vector2.Distance(item.Position, playerPosition);
            if (distance < pickupRange)
            {
                item.IsCollected = true;
                _lootItems.RemoveAt(i);
                // In a full implementation, this would add to inventory
            }
            // Remove expired items
            else if (item.Lifetime <= 0)
            {
                _lootItems.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Hämtar alla loot items för rendering.
    /// </summary>
    public IReadOnlyList<LootItem> GetItems() => _lootItems.AsReadOnly();

    /// <summary>
    /// Hämtar totalt värde på insamlat loot.
    /// </summary>
    public int GetCollectedValue()
    {
        int total = 0;
        foreach (var item in _lootItems)
        {
            total += item.Value;
        }
        return total;
    }

    private string GetRandomItemType()
    {
        int roll = _random.Next(100);
        if (roll < 40) return "gold";
        if (roll < 70) return "health_potion";
        if (roll < 90) return "weapon";
        return "armor";
    }
}
