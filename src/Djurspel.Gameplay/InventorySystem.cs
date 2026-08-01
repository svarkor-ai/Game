using System.Collections.Generic;

namespace Djurspel.Gameplay;

/// <summary>Spelarens inventory med slots och items.</summary>
public class InventorySystem
{
    public record Item(string Name, string Icon, int MaxStack, int Damage, int HealthBonus, bool Equippable);
    public record InventorySlot(Item? Item, int SlotIndex);

    private readonly List<InventorySlot> _slots = new();
    private readonly Dictionary<string, int> _itemCounts = new();
    public int SlotCount { get; } = 20;
    public bool IsOpen { get; set; }

    public InventorySystem()
    {
        for (int i = 0; i < SlotCount; i++)
            _slots.Add(new InventorySlot(null, i));
        
        // Add default items
        AddItem(new Item("Sword", "⚔️", 1, 25, 0, true));
        AddItem(new Item("Shield", "🛡️", 1, 0, 20, true));
        AddItem(new Item("Health Potion", "🧪", 5, 0, 50, false));
        AddItem(new Item("Gold", "💰", 999, 0, 0, false));
    }

    public void AddItem(Item item)
    {
        // Try to stack first
        foreach (var slot in _slots)
        {
            if (slot.Item != null && slot.Item.Name == item.Name && slot.Item.MaxStack > 1)
            {
                // Find actual stack count
                if (_itemCounts.TryGetValue(item.Name, out int count))
                {
                    int newCount = count + item.MaxStack;
                    if (newCount <= item.MaxStack * _slots.FindAll(s => s.Item?.Name == item.Name).Count)
                    {
                        _itemCounts[item.Name] = newCount;
                        return;
                    }
                }
            }
        }
        
        // Add to empty slot
        foreach (var slot in _slots)
        {
            if (slot.Item == null)
            {
                _itemCounts[item.Name] = item.MaxStack;
                _slots[slot.SlotIndex] = new InventorySlot(item, slot.SlotIndex);
                return;
            }
        }
    }

    public InventorySlot[] GetSlots() => _slots.ToArray();
    public Dictionary<string, int> GetItemCounts() => _itemCounts;
}
