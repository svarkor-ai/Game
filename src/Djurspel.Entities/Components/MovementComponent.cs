using Djurspel.Core;

namespace Djurspel.Entities.Components;

public class MovementComponent : IComponent
{
    public float Speed { get; set; } = 3.0f;
    public float CurrentSpeed { get; set; }
    public bool IsMoving { get; set; }
    public Vec3 TargetPosition { get; set; }
}
