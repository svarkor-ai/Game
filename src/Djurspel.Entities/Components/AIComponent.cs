using Djurspel.Core;

namespace Djurspel.Entities.Components;

public enum AIBehavior { Idle, Patrol, Chase, Attack, Flee, Ally }

public class AIComponent : IComponent
{
    public AIBehavior Behavior { get; set; }
    public Vec3I PatrolPoint { get; set; } = Vec3I.Zero;
    public int? TargetEntityId { get; set; }
}
