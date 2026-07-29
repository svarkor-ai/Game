namespace Djurspel.Entities.Components;

public class CombatComponent : IComponent
{
    public float AttackDamage { get; set; }
    public float AttackCooldown { get; set; }
    public float AttackRange { get; set; }
    public float AttackSpeed { get; set; }
    public string WeaponType { get; set; } = "melee";
}
