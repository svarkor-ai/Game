namespace Djurspel.Core;

/// <summary>
/// Types of assets that can be loaded and cached.
/// </summary>
public enum AssetType
{
    Mesh,
    Texture,
    Shader,
    EntityDef,
    LevelDef
}

/// <summary>
/// Texture format for loaded textures.
/// </summary>
public enum TextureFormat
{
    RGB,    // 3 bytes per pixel
    RGBA,   // 4 bytes per pixel
    LUMINANCE,  // 1 byte per pixel
    GRAYSCALE  // 1 byte per pixel (with alpha)
}

/// <summary>
/// Lightweight handle returned by Load.
/// Supports reference counting for lazy unloading.
/// </summary>
public readonly struct AssetHandle<T> where T : notnull
{
    public string Path { get; }
    public T Resource { get; }
    public int RefCount { get; }

    public AssetHandle(string path, T resource, int refCount)
    {
        Path = path;
        Resource = resource;
        RefCount = refCount;
    }
}

/// <summary>
/// Represents a loaded mesh asset with vertex data.
/// Contains VAO ID, element count, and raw mesh data.
/// </summary>
public class MeshAsset
{
    /// <summary>OpenGL VAO ID for this mesh.</summary>
    public int VaoId { get; set; }

    /// <summary>Number of indices/vertices for drawing.</summary>
    public int ElementCount { get; set; }

    /// <summary>Human-readable name for debugging.</summary>
    public string Name { get; set; } = "";

    /// <summary>Vertex positions (float3 per vertex).</summary>
    public float[] Vertices { get; set; } = Array.Empty<float>();

    /// <summary>Vertex normals (float3 per vertex).</summary>
    public float[] Normals { get; set; } = Array.Empty<float>();

    /// <summary>Texture UV coordinates (float2 per vertex).</summary>
    public float[] Uv { get; set; } = Array.Empty<float>();

    /// <summary>Index buffer (uint or int array).</summary>
    public int[] Indices { get; set; } = Array.Empty<int>();
}

/// <summary>
/// Represents a loaded texture asset.
/// Contains OpenGL handle and metadata.
/// </summary>
public class TextureAsset
{
    /// <summary>OpenGL texture handle (GL uint).</summary>
    public int GlHandle { get; set; }

    /// <summary>Texture width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Texture height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Format of the texture pixel data.</summary>
    public TextureFormat Format { get; set; }
}

/// <summary>
/// Represents a compiled shader program.
/// Contains GL program ID and uniform locations.
/// </summary>
public class ShaderProgram
{
    /// <summary>OpenGL program ID.</summary>
    public int GlProgramId { get; set; }

    /// <summary>Cached uniform locations for fast lookup.</summary>
    public Dictionary<string, int> UniformLocations { get; } = new();
}

/// <summary>
/// Interface for the central asset manager.
/// Handles loading, caching, and unloading of all game assets.
/// Thread-safe for reads; all writes go through this singleton.
/// </summary>
public interface IAssetManager
{
    /// <summary>Load an asset by path. Returns cached instance if already loaded.</summary>
    T Load<T>(string path) where T : notnull;

    /// <summary>Check if an asset is in cache.</summary>
    bool Contains<T>(string path) where T : notnull;

    /// <summary>Remove an asset from cache. If refcount=0, dispose native resources.</summary>
    void Unload<T>(string path) where T : notnull;

    /// <summary>Unload all assets of a given type. Called on scene change.</summary>
    void UnloadAll<T>() where T : notnull;

    /// <summary>Unload every cached asset. Called on shutdown.</summary>
    void ClearAll();
}
