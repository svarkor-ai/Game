using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace Djurspel.Graphics;

public class IsometricCamera : ICamera
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public float Zoom { get; set; } = 1.0f;

    public Vector2 TileToScreen(Vector3i tile, float tilePixelWidth = 64f, float tilePixelHeight = 32f)
    {
        float halfW = tilePixelWidth * 0.5f;
        float halfH = tilePixelHeight * 0.5f;
        float screenX = (tile.X - tile.Y) * halfW;
        float screenY = (tile.X + tile.Y) * halfH - tile.Z * tilePixelHeight;
        return new Vector2(screenX, screenY);
    }

    public Vector3i ScreenToTile(Vector2 screen, float tilePixelWidth = 64f, float tilePixelHeight = 32f)
    {
        float halfW = tilePixelWidth * 0.5f;
        float halfH = tilePixelHeight * 0.5f;
        float tileX = (screen.X / halfW + screen.Y / halfH) * 0.5f;
        float tileY = (screen.Y / halfH - screen.X / halfW) * 0.5f;
        return new Vector3i((int)tileX, (int)tileY, 0);
    }

    public Vector2 WorldToScreen(Vector3 worldPos)
    {
        var tile = new Vector3i((int)worldPos.X, (int)worldPos.Y, (int)worldPos.Z);
        var screen = TileToScreen(tile);
        float subX = worldPos.X - tile.X;
        float subY = worldPos.Y - tile.Y;
        float halfW = 32f;
        float halfH = 16f;
        return new Vector2(screen.X + (subX - subY) * halfW, screen.Y + (subX + subY) * halfH);
    }

    public Vector3 ScreenToWorld(Vector2 screenPos)
    {
        var tile = ScreenToTile(screenPos);
        return new Vector3(tile.X, tile.Y, tile.Z);
    }

    public IEnumerable<Vector3i> GetDepthSortedOrder(IEnumerable<Vector3i> entities)
    {
        return entities.OrderByDescending(e => e.X + e.Y);
    }

    public void FollowEntity(object target, float smoothingFactor = 5f, float dt = 0.016f)
    {
        // Stub — target must have TransformComponent
        // Implementation in gameplay phase
    }
}
