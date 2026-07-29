namespace Djurspel.Entities.Components;

using Djurspel.Core;

public class TransformComponent : IComponent
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Scale { get; set; } = 1.0f;
    public float RotationY { get; set; } = 0f;

    public Vec3 ToVec3() => new Vec3(X, Y, Z);
}
