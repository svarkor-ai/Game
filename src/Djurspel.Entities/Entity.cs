using System.Collections.Generic;

namespace Djurspel.Entities;

public class Entity : IDisposable
{
    private readonly Dictionary<System.Type, IComponent> _components = new();
    private bool _disposed;
    private bool _dead;

    public int Id { get; internal set; }
    public string Name { get; set; } = "Entity";

    public bool IsAlive => !_dead;

    public void AddComponent<T>(T component) where T : IComponent
    {
        if (_disposed || _dead) throw new System.ObjectDisposedException(nameof(Entity));
        if (component == null) throw new System.ArgumentNullException(nameof(component));
        _components[typeof(T)] = component;
    }

    public T? GetComponent<T>() where T : IComponent
    {
        if (_disposed) return default;
        return _components.TryGetValue(typeof(T), out var c) ? (T)c : default;
    }

    public void RemoveComponent<T>() where T : IComponent
    {
        _components.Remove(typeof(T));
    }

    public System.Collections.Generic.IEnumerable<T> GetComponents<T>() where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out var c)) yield return (T)c;
    }

    public void Die()
    {
        _dead = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _components.Clear();
        _disposed = true;
    }
}
