using Djurspel.Core;
using OpenTK.Mathematics;

namespace Djurspel.Graphics;

/// <summary>
/// OpenGL shader program — holds the GL program handle and cached uniform locations.
/// </summary>
public class ShaderProgram
{
    public int GlProgramId { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<string, int> UniformLocations { get; } = new();
}

/// <summary>
/// Manages compilation, linking, and uniform uploads for GLSL shaders.
/// </summary>
public interface IShaderManager : IDisposable
{
    ShaderProgram Load(string name, string vertexSource, string fragmentSource, string? geometrySource = null);
    ShaderProgram? Get(string name);
    void Bind(ShaderProgram shader);
    void SetFloat(string name, float value);
    void SetVec3(string name, Vector3 value);
    void SetVec4(string name, Vector4 value);
    void SetMat4(string name, float[] matrix);
    void SetInt(string name, int value);
}
