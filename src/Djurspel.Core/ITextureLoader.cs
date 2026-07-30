namespace Djurspel.Core;

/// <summary>
/// Loads texture images into OpenGL texture handles.
/// </summary>
public interface ITextureLoader : IDisposable
{
    /// <summary>Load a texture from file. Returns cached if already loaded.</summary>
    TextureAsset LoadTexture(string path);
}
