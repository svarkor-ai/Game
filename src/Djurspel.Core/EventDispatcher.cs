namespace Djurspel.Core;

/// <summary>
/// Global event dispatcher implementing pub/sub pattern.
/// All subsystems communicate via events — no hard-coded references.
/// Handlers execute in priority order (Critical → High → Normal → Low).
/// Thread-safe for concurrent dispatches; dispatches are processed synchronously.
/// </summary>
public sealed class EventDispatcher : IEventDispatcher, IDisposable
{
    private static readonly EventDispatcher _instance = new();
    public static EventDispatcher Instance => _instance;

    private readonly Dictionary<Type, List<HandlerBase>> _handlers = new();
    private readonly HashSet<HandlerBase> _subscriptions = new();
    private readonly object _lock = new();
    private volatile bool _isDispatching = false;

    private EventDispatcher() { }

    public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal)
        where T : IEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var sub = new TypedSubscription<T>(this, handler, priority);
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var hList))
            {
                hList = new List<HandlerBase>();
                _handlers[typeof(T)] = hList;
            }
            hList.Add(sub);
            hList.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            _subscriptions.Add(sub);
        }
        return sub;
    }

    public IDisposable SubscribeOnce<T>(Action<T> handler) where T : IEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return new OneShotSubscription<T>(this, handler);
    }

    public void Dispatch<T>(T evt) where T : IEvent
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (_isDispatching) throw new InvalidOperationException("Re-entrant dispatch detected");
        List<HandlerBase>? handlers = null;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var h)) return;
            handlers = new List<HandlerBase>(h);
        }
        _isDispatching = true;
        try
        {
            foreach (var h in handlers)
            {
                if (!h.IsValid) continue;
                var t = h as TypedSubscription<T>;
                if (t != null) t.Invoke(evt);
            }
        }
        finally { _isDispatching = false; }
    }

     public void DispatchForEntity<T>(int targetEntityId, T evt) where T : IEvent
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (_isDispatching) throw new InvalidOperationException("Re-entrant dispatch detected");
        List<HandlerBase>? handlers = null;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var h)) return;
            handlers = new List<HandlerBase>(h);
        }
        _isDispatching = true;
        try
        {
            foreach (var h in handlers)
            {
                if (!h.IsValid) continue;
                var t = h as TypedSubscription<T>;
                if (t != null && t.TargetEntityId == targetEntityId) t.Invoke(evt);
            }
        }
        finally { _isDispatching = false; }
    }

    public void UnsubscribeAll<T>() where T : IEvent
    {
        lock (_lock) { _handlers.Remove(typeof(T)); }
    }

    public void ClearAll()
    {
        lock (_lock) { _handlers.Clear(); _subscriptions.Clear(); }
    }

    public bool HasSubscribers<T>() where T : IEvent
    {
        lock (_lock) return _handlers.TryGetValue(typeof(T), out var h) && h.Count > 0;
    }

    public int GetSubscriberCount<T>() where T : IEvent
    {
        lock (_lock) return _handlers.TryGetValue(typeof(T), out var h) ? h.Count : 0;
    }

    public void Dispose()
    {
        lock (_lock) { _handlers.Clear(); _subscriptions.Clear(); }
    }

    // ── Internal types ─────────────────────────────────────────────

    private abstract class HandlerBase
    {
        public abstract EventPriority Priority { get; }
        public abstract bool IsValid { get; }
    }

    private class TypedSubscription<T> : HandlerBase, IDisposable where T : IEvent
    {
        private readonly EventDispatcher _dd;
        private readonly Action<T> _handler;
        private bool _disposed = false;

        public override EventPriority Priority { get; }
        public override bool IsValid => !_disposed;
        public int? TargetEntityId { get; }

        public TypedSubscription(EventDispatcher dd, Action<T> handler,
            EventPriority priority, int? targetEntityId = null)
        {
            _dd = dd; _handler = handler; Priority = priority;
            TargetEntityId = targetEntityId;
        }

        public virtual void Invoke(T evt) => _handler(evt);

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dd._subscriptions.Remove(this);
            }
        }
    }

    private class OneShotSubscription<T> : TypedSubscription<T> where T : IEvent
    {
        public OneShotSubscription(EventDispatcher dd, Action<T> handler)
            : base(dd, handler, EventPriority.Normal) { }

        public override void Invoke(T evt)
        {
            base.Invoke(evt);
            Dispose();
        }
    }
}
