using Djurspel.Core;

namespace Djurspel.Graphics;

public interface IPrimitiveMeshFactory
{
    MeshAsset CreateCube();
    MeshAsset CreateSphere(int segments = 16);
    MeshAsset CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 16);
    MeshAsset CreatePlane(float width = 1.0f, float depth = 1.0f);
    MeshAsset CreateIsometricTile(float width, float depth, float height);
}
