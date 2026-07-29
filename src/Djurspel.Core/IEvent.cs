namespace Djurspel.Core;

/// <summary>
/// Priority levels for event handling.
/// Higher priority handlers execute first.
/// </summary>
public enum EventPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Marker interface for all events in the game.
/// Guarantees type safety in the event system.
/// </summary>
public interface IEvent
{
}

/// <summary>
/// Base class for events associated with a specific entity.
/// Provides timestamp and source entity ID.
/// </summary>
public abstract class EntityEvent : IEvent
{
    public DateTime Timestamp { get; }
    public int SourceEntityId { get; }

    protected EntityEvent(int sourceEntityId)
    {
        Timestamp = DateTime.UtcNow;
        SourceEntityId = sourceEntityId;
    }
}

/// <summary>
/// Base class for events not tied to a specific entity.
/// </summary>
public sealed class GameEvent : IEvent
{
    public DateTime Timestamp { get; }

    public GameEvent()
    {
        Timestamp = DateTime.UtcNow;
    }
}
