using System.Collections.Generic;
using System.Linq;

namespace Djurspel.Gameplay;

/// <summary>
/// Stub implementation of IInventoryManager.
/// Full implementation will be added when inventory systems are developed.
/// </summary>
public class InventoryManager : IInventoryManager
{
    private readonly List<InventorySlot> _items = new();

    public void AddItem(string itemId, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.ItemId == itemId);
        if (existing.ItemId == itemId)
            existing.Quantity += quantity;
        else
            _items.Add(new InventorySlot { ItemId = itemId, Quantity = quantity });
    }

    public void RemoveItem(string itemId, int quantity)
    {
        var slot = _items.FirstOrDefault(i => i.ItemId == itemId);
        if (string.IsNullOrEmpty(slot.ItemId)) return;

        slot.Quantity -= quantity;
        if (slot.Quantity <= 0)
            _items.Remove(slot);
    }

    public IEnumerable<InventorySlot> GetItems() => _items;

    public void Dispose()
    {
        _items.Clear();
    }
}
