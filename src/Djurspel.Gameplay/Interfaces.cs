using Djurspel.Core;
using OpenTK.Mathematics;

namespace Djurspel.Gameplay;

// ---- Event types ----

public record MoveQueuedEvent(int EntityId, Vec2 ScreenTarget) : IEvent;
public record AttackQueuedEvent(int EntityId, Vec2 ScreenTarget) : IEvent;
public record AbilityQueuedEvent(int EntityId, int AbilityId) : IEvent;
public record AttackEvent(int AttackerId, int? TargetId, Vec3 AttackerPos) : IEvent;
public record DamageDealtEvent(int TargetId, float Amount) : IEvent;
public record EntityDiedEvent(int EntityId) : IEvent;
public record EnemyKilledEvent(int TargetId, int KillerId) : IEvent;
public record AIUpdateEvent(float DeltaTime) : IEvent;
public record AIActionEvent(int AEntityId, string Action, Vec3 TargetPos) : IEvent;
public record TileChangedEvent(int X, int Y, int Z) : IEvent;

// ---- Interfaces ----

/// <summary>Manages player input → actions → events.</summary>
public interface IInputManager : IDisposable
{
    void ProcessFrame();
}
