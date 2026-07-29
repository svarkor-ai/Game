namespace Djurspel.Entities.Components;

public class LootComponent : IComponent
{
    public string[] LootTable { get; set; } = System.Array.Empty<string>();
    public float DropChance { get; set; } = 1.0f;
}
