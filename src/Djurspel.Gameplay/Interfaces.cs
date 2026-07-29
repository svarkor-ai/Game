using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;
using OpenTK.Mathematics;

namespace Djurspel.Gameplay;

// ---- Event types ----

public record MoveQueuedEvent(int EntityId, Vec2 ScreenTarget) : IEvent;
public record AttackQueuedEvent(int EntityId, Vec2 ScreenTarget) : IEvent;
public record AbilityQueuedEvent(int EntityId, int AbilityId) : IEvent;

/// <summary>Player attacks a target.</summary>
public record AttackEvent(int AttackerId, int? TargetId, Vec3 AttackerPos) : IEvent;

/// <summary>Damage was dealt to an entity.</summary>
public record DamageDealtEvent(int TargetId, float Amount) : IEvent;

/// <summary>Entity died.</summary>
public record EntityDiedEvent(int EntityId) : IEvent;

/// <summary>Enemy was killed, drops loot and gives XP.</summary>
public record EnemyKilledEvent(int TargetId, int KillerId) : IEvent;

/// <summary>AI tick — update all AI entities.</summary>
public record AIUpdateEvent(float DeltaTime) : IEvent;

/// <summary>AI entity takes action.</summary>
public record AIActionEvent(int AEntityId, string Action, Vec3 TargetPos) : IEvent;

/// <summary>Tile was changed in the world.</summary>
public record TileChangedEvent(int X, int Y, int Z) : IEvent;

// ---- Interfaces ----

/// <summary>Handles combat logic: attacks, damage, cooldowns, kills.</summary>
public interface ICombatManager : IDisposable
{
    CombatResult Attack(int attackerEntityId, int? targetEntityId, Vec3 attackerPos);
    void Update(float dt);
    CombatResult AiAttack(int attackerEntityId, int targetEntityId);
}

/// <summary>Handles AI behavior updates for entities with AIComponent.</summary>
public interface IAIManager : IDisposable
{
    void Update(float dt);
    void SetTarget(int aiEntityId, int targetEntityId);
    void Remove(int aiEntityId);
}

/// <summary>Manages player input → actions → events.</summary>
public interface IInputManager : IDisposable
{
    void QueueMove(Vector2 screenTarget);
    void QueueAttack(Vector2 screenTarget);
    void QueueAbility(int abilityId);
    void QueueMoralChoice(Core.MoralAlignment choice, int companionId);
    void ClearQueues();
    void ProcessFrame();
}

/// <summary>Manages the moral choice system.</summary>
public interface IMoralManager : IDisposable
{
    void RecordDecision(int decisionId, Core.MoralAlignment choice, int? companionAffected);
    MoralScore GetScore();
    bool TriggersBetrayal(int decisionId, int companionId);
}

public struct MoralScore
{
    public int Compassionate { get; set; }
    public int Ruthless { get; set; }
    public Core.MoralAlignment Dominant =>
        Compassionate > Ruthless ? Core.MoralAlignment.Compassionate :
        Ruthless > Compassionate ? Core.MoralAlignment.Ruthless : Core.MoralAlignment.Neutral;
}

/// <summary>Manages inventory (stubbed for now).</summary>
public interface IInventoryManager : IDisposable
{
    void AddItem(string itemId, int quantity);
    void RemoveItem(string itemId, int quantity);
    IEnumerable<InventorySlot> GetItems();
}

public struct InventorySlot
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }
    public string? EquippedToEntityId { get; set; }
}
