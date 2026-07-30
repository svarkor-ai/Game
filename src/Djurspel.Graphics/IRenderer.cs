using OpenTK.Mathematics;
using Djurspel.Core;

namespace Djurspel.Graphics;

public interface IRenderer : IDisposable
{
    void Initialize();
    void SetShaderManager(IShaderManager shaderManager);
    void Render(ICamera camera, IShaderManager shaderManager, float frameTime);
    void DrawCube(Vector3 position, Vector3 size, Vector4 color, IShaderManager shaderManager);
    void DrawSphere(Vector3 position, float radius, Vector4 color, IShaderManager shaderManager);
    void DrawCylinder(Vector3 position, float radius, float height, Vector4 color, IShaderManager shaderManager);
    void DrawPlane(Vector3 position, Vector2 size, Vector4 color, IShaderManager shaderManager);

    // Game scene methods — use object params to avoid circular deps
    void BeginScene();
    void EndScene();
    void DrawTileMap(object world, object region, float interpolation);
    void DrawEntity(object entity, float interpolation);
}
