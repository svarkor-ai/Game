namespace Djurspel.Core;

/// <summary>
/// Interface for the event dispatcher system.
/// Enables decoupled communication between game systems.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>Subscribe to events of type T.</summary>
    System.IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IEvent;

    /// <summary>Subscribe once — auto-unsubscribes after first event.</summary>
    System.IDisposable SubscribeOnce<T>(Action<T> handler) where T : IEvent;

    /// <summary>Dispatch an event to all subscribers.</summary>
    void Dispatch<T>(T evt) where T : IEvent;

    /// <summary>Dispatch an event only to subscribers of a specific entity.</summary>
    void DispatchForEntity<T>(int targetEntityId, T evt) where T : IEvent;

    /// <summary>Unsubscribe all handlers for an event type.</summary>
    void UnsubscribeAll<T>() where T : IEvent;

    /// <summary>Clear all subscribers.</summary>
    void ClearAll();

    /// <summary>Check if there are subscribers for an event type.</summary>
    bool HasSubscribers<T>() where T : IEvent;

    /// <summary>Get subscriber count for an event type.</summary>
    int GetSubscriberCount<T>() where T : IEvent;
}
