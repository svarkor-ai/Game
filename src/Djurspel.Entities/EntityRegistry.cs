using System.Collections.Generic;

namespace Djurspel.Entities;

public interface IEntityRegistry : IDisposable
{
    Entity Create();
    Entity CreateFromDefinition(EntityDefinition def);
    Entity? Get(int id);
    IEnumerable<Entity> Query<T>() where T : IComponent;
    IEnumerable<int> GetAllLivingIds();
    List<int> ProcessDeaths();
}

public class EntityRegistry : IEntityRegistry
{
    private readonly Dictionary<int, Entity> _entities = new();
    private int _nextId = 1;
    private bool _disposed;

    public Entity Create()
    {
        if (_disposed) throw new System.ObjectDisposedException(nameof(EntityRegistry));
        var entity = new Entity { Id = _nextId++, Name = $"Entity{_nextId - 1}" };
        _entities[entity.Id] = entity;
        return entity;
    }

    public Entity CreateFromDefinition(EntityDefinition def)
    {
        var entity = Create();
        entity.Name = def.Name;
        return entity;
    }

    public Entity? Get(int id) => _entities.TryGetValue(id, out var e) ? e : null;

    public IEnumerable<Entity> Query<T>() where T : IComponent
    {
        foreach (var entity in _entities.Values)
        {
            if (entity.IsAlive && entity.GetComponent<T>() != null) yield return entity;
        }
    }

    public IEnumerable<int> GetAllLivingIds()
    {
        foreach (var kvp in _entities)
        {
            if (kvp.Value.IsAlive) yield return kvp.Key;
        }
    }

    public List<int> ProcessDeaths()
    {
        var dead = new List<int>();
        foreach (var kvp in _entities)
        {
            if (!kvp.Value.IsAlive)
            {
                dead.Add(kvp.Key);
                kvp.Value.Dispose();
                _entities.Remove(kvp.Key);
            }
        }
        return dead;
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var entity in _entities.Values)
        {
            entity.Dispose();
        }
        _entities.Clear();
        _disposed = true;
    }
}
