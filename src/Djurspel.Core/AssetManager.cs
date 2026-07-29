using System.Collections.Concurrent;

namespace Djurspel.Core;

/// <summary>
/// Central asset loader and cache manager.
/// Thread-safe for concurrent reads; all writes go through this singleton.
/// Uses ConcurrentDictionary for the cache and per-asset reference counting.
/// 
/// Design notes:
/// - Load<T> increments refcount automatically
/// - Unload<T> decrements refcount; only removes when refcount reaches 0
/// - No actual file I/O in this version — that will be added per-asset-type later
/// - The interface is designed to accept IAssetLoader implementations for different asset types
/// </summary>
public sealed class AssetManager : IAssetManager, IDisposable
{
    /// <summary>
    /// Static singleton instance for global access.
    /// </summary>
    private static readonly AssetManager _instance = new();

    /// <summary>
    /// Get the global AssetManager instance.
    /// </summary>
    public static AssetManager Instance => _instance;

    /// <summary>
    /// Thread-safe cache of all loaded assets.
    /// Key: "TypeName::path" (e.g., "MeshAsset::assets/meshes/cube.obj")
    /// Value: the asset object
    /// </summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    /// Per-type caches for fast UnloadAll<T> and Contains<T> checks.
    /// </summary>
    private readonly ConcurrentDictionary<Type, HashSet<string>> _typeKeys = new();

    /// <summary>
    /// Registered asset loaders, keyed by asset type.
    /// Each loader knows how to deserialize a specific asset type from disk.
    /// </summary>
    private readonly Dictionary<Type, IAssetLoader> _loaders = new();

    /// <summary>
    /// Whether the manager has been disposed.
    /// </summary>
    private bool _disposed = false;

    private AssetManager()
    {
    }

    /// <summary>
    /// Register a loader for a specific asset type.
    /// Call this during initialization to register type-specific loaders.
    /// </summary>
    public void RegisterLoader<T>(IAssetLoader<T> loader) where T : notnull
    {
        var type = typeof(T);
        lock (_loaders)
        {
            _loaders[type] = loader;
        }
    }

    /// <summary>
    /// Load an asset by path. Returns cached instance if already loaded.
    /// Increments the asset's reference count automatically.
    /// If not in cache, attempts to load via the registered loader for type T.
    /// </summary>
    public T Load<T>(string path) where T : notnull
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        var key = GetCacheKey<T>(path);
        var existing = _cache.GetOrAdd(key, entryKey =>
        {
            // Asset not in cache — load it
            T? asset = TryLoad<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException(
                    $"Asset not found: {path} (type: {typeof(T).Name})");
            }

            var typeKeys = _typeKeys.GetOrAdd(typeof(T), _ => new HashSet<string>());
            lock (typeKeys)
            {
                typeKeys.Add(entryKey);
            }

            return new CacheEntry(asset, 1);
        });

        // Increment refcount (concurrency-safe)
        Interlocked.Increment(ref existing.RefCount);
        return (T)existing.Resource;
    }

    /// <summary>
    /// Check if an asset of the given type is in cache at the specified path.
    /// </summary>
    public bool Contains<T>(string path) where T : notnull
    {
        var key = GetCacheKey<T>(path);
        return _cache.ContainsKey(key);
    }

    /// <summary>
    /// Unload an asset by path. Decrements the reference count.
    /// Only removes the asset from cache when refcount reaches 0.
    /// If the loader supports disposal, calls Dispose on the asset.
    /// </summary>
    public void Unload<T>(string path) where T : notnull
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        var key = GetCacheKey<T>(path);
        if (_cache.TryRemove(key, out var entry))
        {
            Interlocked.Decrement(ref entry.RefCount);

            // Remove from type keys
            var typeKeys = _typeKeys[typeof(T)];
            lock (typeKeys)
            {
                typeKeys.Remove(key);
            }

            // Dispose if the asset supports it
            if (entry.Resource is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Unload all assets of a given type. Called on scene change.
    /// Disposes each asset and removes it from cache.
    /// </summary>
    public void UnloadAll<T>() where T : notnull
    {
        var typeKeys = _typeKeys.GetOrAdd(typeof(T), _ => new HashSet<string>());
        lock (typeKeys)
        {
            foreach (var key in typeKeys.ToArray())
            {
                if (_cache.TryRemove(key, out var entry))
                {
                    if (entry.Resource is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            typeKeys.Clear();
        }
    }

    /// <summary>
    /// Unload every cached asset. Called on shutdown.
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in _cache)
        {
            if (kvp.Value.Resource is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _cache.Clear();
        _typeKeys.Clear();
    }

    /// <summary>
    /// Get the total number of cached assets across all types.
    /// </summary>
    public int TotalCacheCount => _cache.Count;

    /// <summary>
    /// Get the number of cached assets for a given type.
    /// </summary>
    public int GetTypeCacheCount<T>() where T : notnull
    {
        var typeKeys = _typeKeys.GetOrAdd(typeof(T), _ => new HashSet<string>());
        lock (typeKeys)
        {
            return typeKeys.Count;
        }
    }

    /// <summary>
    /// Try to load an asset from disk using the registered loader.
    /// </summary>
    private T? TryLoad<T>(string path) where T : notnull
    {
        var type = typeof(T);
        IAssetLoader? loader;

        lock (_loaders)
        {
            if (!_loaders.TryGetValue(type, out loader))
            {
                // No loader registered — return default (will throw)
                return default;
            }
        }

        return (T)loader.Load(path);
    }

    /// <summary>
    /// Generate a cache key from type and path.
    /// Format: "TypeName::path" — unique per type/path combination.
    /// </summary>
    private static string GetCacheKey<T>(string path) where T : notnull
    {
        return $"{typeof(T).Name}::{path}";
    }

    /// <summary>
    /// Dispose the asset manager and all cached resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ClearAll();
    }

    /// <summary>
    /// Internal cache entry with reference counting.
    /// </summary>
    private sealed class CacheEntry
    {
        public object Resource { get; }
        public int RefCount;

        public CacheEntry(object resource, int refCount)
        {
            Resource = resource;
            RefCount = refCount;
        }
    }
}

/// <summary>
/// Interface for asset type loaders.
/// Implementations know how to load a specific asset type from disk.
/// </summary>
public interface IAssetLoader
{
    /// <summary>Load an asset from the given path.</summary>
    object Load(string path);
}

/// <summary>
/// Generic asset loader interface for type-safe implementations.
/// </summary>
public interface IAssetLoader<T> : IAssetLoader where T : notnull
{
    /// <summary>Load an asset from the given path.</summary>
    new T Load(string path);

    object IAssetLoader.Load(string path) => Load(path);
}
