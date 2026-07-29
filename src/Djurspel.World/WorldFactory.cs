using Djurspel.Core;
using Djurspel.Graphics;

namespace Djurspel.World;

/// <summary>
/// Factory for creating worlds programmatically.
/// </summary>
public class WorldFactory
{
    /// <summary>
    /// Creates a simple flat world with the given dimensions.
    /// </summary>
    public static IWorld CreateFromPrimitive(int width, int height, int layers, TileType defaultTile)
    {
        return new PrimitiveWorld(width, height, layers, defaultTile);
    }
}