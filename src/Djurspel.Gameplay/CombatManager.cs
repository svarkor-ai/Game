using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.Entities.Components;

namespace Djurspel.Gameplay;

/// <summary>
/// Concrete implementation of ICombatManager — processes attacks and handles combat events.
/// </summary>
public class CombatManager : ICombatManager
{
    private readonly IEntityRegistry _registry;
    private readonly IEventDispatcher _dispatcher;

    public CombatManager(IEntityRegistry registry, IEventDispatcher dispatcher)
    {
        _registry = registry;
        _dispatcher = dispatcher;
    }

    public CombatResult Attack(int attackerEntityId, int? targetEntityId, Vec3 attackerPos)
    {
        var attacker = _registry.Get(attackerEntityId);
        if (attacker == null)
            return new CombatResult { TargetId = targetEntityId ?? -1, Hit = false, DamageDealt = 0f };

        var combat = attacker.GetComponent<CombatComponent>();
        if (combat == null)
            return new CombatResult { TargetId = targetEntityId ?? -1, Hit = false, DamageDealt = 0f };

        if (targetEntityId == null)
            return new CombatResult { TargetId = -1, Hit = false, DamageDealt = 0f };

        var target = _registry.Get(targetEntityId.Value);
        if (target == null)
            return new CombatResult { TargetId = targetEntityId.Value, Hit = false, DamageDealt = 0f };

        var health = target.GetComponent<HealthComponent>();
        if (health == null)
            return new CombatResult { TargetId = targetEntityId.Value, Hit = false, DamageDealt = 0f };

        var targetTransform = target.GetComponent<TransformComponent>();
        float distance = attackerPos.DistanceTo(targetTransform != null ? targetTransform.ToVec3() : Vec3.Zero);

        if (distance > combat.AttackRange)
            return new CombatResult { TargetId = targetEntityId.Value, Hit = false, DamageDealt = 0f };

        float damage = combat.AttackDamage;
        health.Current -= damage;
        bool killed = health.Current <= 0f;

        _dispatcher.Dispatch(new DamageDealtEvent(targetEntityId.Value, damage));

        if (killed)
        {
            health.Current = 0f;
            target.Die();
            _dispatcher.Dispatch(new EnemyKilledEvent(targetEntityId.Value, attackerEntityId));
            _dispatcher.Dispatch(new EntityDiedEvent(targetEntityId.Value));
        }

        return new CombatResult
        {
            TargetId = targetEntityId.Value,
            DamageDealt = damage,
            Hit = true,
            Killed = killed
        };
    }

    public void Update(float dt)
    {
        // Process cooldowns for all combat entities — stubbed for now.
        foreach (var e in _registry.Query<CombatComponent>())
        {
            // Cooldown logic would go here in full implementation.
        }
    }

    public CombatResult AiAttack(int attackerEntityId, int targetEntityId)
    {
        var attacker = _registry.Get(attackerEntityId);
        if (attacker == null)
            return new CombatResult { TargetId = targetEntityId, Hit = false, DamageDealt = 0f };

        var combat = attacker.GetComponent<CombatComponent>();
        if (combat == null)
            return new CombatResult { TargetId = targetEntityId, Hit = false, DamageDealt = 0f };

        var target = _registry.Get(targetEntityId);
        if (target == null)
            return new CombatResult { TargetId = targetEntityId, Hit = false, DamageDealt = 0f };

        var health = target.GetComponent<HealthComponent>();
        if (health == null)
            return new CombatResult { TargetId = targetEntityId, Hit = false, DamageDealt = 0f };

        float damage = combat.AttackDamage;
        health.Current -= damage;
        bool killed = health.Current <= 0f;

        _dispatcher.Dispatch(new DamageDealtEvent(targetEntityId, damage));

        if (killed)
        {
            health.Current = 0f;
            target.Die();
            _dispatcher.Dispatch(new EntityDiedEvent(targetEntityId));
        }

        return new CombatResult
        {
            TargetId = targetEntityId,
            DamageDealt = damage,
            Hit = true,
            Killed = killed
        };
    }

    public void Dispose() { }
}
