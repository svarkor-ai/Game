namespace Djurspel.Gameplay;

public struct CombatResult
{
    public int TargetId { get; set; }
    public float DamageDealt { get; set; }
    public bool Hit { get; set; }
    public bool Killed { get; set; }
}
