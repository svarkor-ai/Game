using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.Entities.Components;
using Djurspel.World;

namespace Djurspel.Gameplay;

/// <summary>
/// Concrete implementation of IAIManager — updates AI entities and handles behavior.
/// </summary>
public class AIManager : IAIManager
{
    private readonly IEntityRegistry _registry;
    private readonly IWorld _world;
    private readonly IEventDispatcher _dispatcher;
    private readonly HashSet<int> _activeAIs = new();

    public AIManager(IEntityRegistry registry, IWorld world, IEventDispatcher dispatcher)
    {
        _registry = registry;
        _world = world;
        _dispatcher = dispatcher;
    }

    public void Update(float dt)
    {
        foreach (var entity in _registry.Query<AIComponent>())
        {
            if (!_activeAIs.Contains(entity.Id))
                _activeAIs.Add(entity.Id);

            var ai = entity.GetComponent<AIComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            var movement = entity.GetComponent<MovementComponent>();

            if (ai == null || transform == null || movement == null)
                continue;

            UpdateAI(ai, entity, transform, movement, dt);
        }
    }

    private void UpdateAI(AIComponent ai, Entity entity, TransformComponent transform, MovementComponent movement, float dt)
    {
        switch (ai.Behavior)
        {
            case AIBehavior.Idle:
                movement.CurrentSpeed = 0f;
                break;

            case AIBehavior.Patrol:
                if (ai.PatrolPoint != Vec3I.Zero)
                {
                    MoveToward(transform, movement, ai.PatrolPoint.X, ai.PatrolPoint.Z, dt);
                    if (Distance2D(transform.X, transform.Z, ai.PatrolPoint.X, ai.PatrolPoint.Z) < 0.5f)
                    {
                        ai.PatrolPoint = Vec3I.Zero;
                        ai.Behavior = AIBehavior.Idle;
                    }
                }
                break;

            case AIBehavior.Chase:
                if (ai.TargetEntityId.HasValue)
                {
                    var target = _registry.Get(ai.TargetEntityId.Value);
                    if (target != null)
                    {
                        var targetTransform = target.GetComponent<TransformComponent>();
                        if (targetTransform != null)
                        {
                            MoveToward(transform, movement, targetTransform.X, targetTransform.Z, dt);
                            _dispatcher.Dispatch(new AIActionEvent(entity.Id, "Chase", targetTransform.ToVec3()));
                        }
                    }
                    else
                    {
                        ai.TargetEntityId = null;
                        ai.Behavior = AIBehavior.Idle;
                    }
                }
                break;

            case AIBehavior.Flee:
                // Flee away from target — stubbed
                break;
        }
    }

    private static float Distance2D(float x1, float z1, int tx, int tz)
    {
        float dx = tx - x1;
        float dz = tz - z1;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private void MoveToward(TransformComponent transform, MovementComponent movement, float tx, float tz, float dt)
    {
        float dx = tx - transform.X;
        float dz = tz - transform.Z;
        float dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist > 0.1f)
        {
            float speed = Math.Min(movement.Speed * dt, dist);
            transform.X += (dx / dist) * speed;
            transform.Z += (dz / dist) * speed;
            movement.IsMoving = true;
            movement.CurrentSpeed = movement.Speed;
        }
        else
        {
            movement.IsMoving = false;
            movement.CurrentSpeed = 0f;
        }
    }

    public void SetTarget(int aiEntityId, int targetEntityId)
    {
        var ai = _registry.Get(aiEntityId);
        if (ai == null) return;

        var aiComp = ai.GetComponent<AIComponent>();
        if (aiComp != null)
        {
            aiComp.TargetEntityId = targetEntityId;
            aiComp.Behavior = AIBehavior.Chase;
        }
    }

    public void Remove(int aiEntityId)
    {
        _activeAIs.Remove(aiEntityId);
    }

    public void Dispose()
    {
        _activeAIs.Clear();
    }
}
