using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Djurspel.Graphics;

public interface ICamera
{
    Vector3 Position { get; set; }
    float Zoom { get; set; }

    Vector2 WorldToScreen(Vector3 worldPos);
    Vector3 ScreenToWorld(Vector2 screenPos);
    Vector2 TileToScreen(Vector3i tile, float tilePixelWidth = 64f, float tilePixelHeight = 32f);
    Vector3i ScreenToTile(Vector2 screen, float tilePixelWidth = 64f, float tilePixelHeight = 32f);
    IEnumerable<Vector3i> GetDepthSortedOrder(IEnumerable<Vector3i> entities);
    void FollowEntity(object target, float smoothingFactor = 5f, float dt = 0.016f);
}
