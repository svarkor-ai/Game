namespace Djurspel.Entities.Components;

public class HealthComponent : IComponent
{
    public float Current { get; set; }
    public float Max { get; set; }
    public bool IsDead => Current <= 0f;
}
