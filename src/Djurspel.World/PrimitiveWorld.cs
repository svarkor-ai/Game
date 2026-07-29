using System.Collections.Generic;
using Djurspel.Core;
using Djurspel.Graphics;

namespace Djurspel.World;

/// <summary>
/// Simple flat world implementation for testing.
/// All tiles have the same type.
/// </summary>
public class PrimitiveWorld : IWorld
{
    private readonly TileType _defaultType;
    private readonly int _width;
    private readonly int _height;
    private readonly int _layers;

    public int Width => _width;
    public int Height => _height;
    public int Layers => _layers;

    public PrimitiveWorld(int width, int height, int layers, TileType defaultType)
    {
        _width = width;
        _height = height;
        _layers = layers;
        _defaultType = defaultType;
    }

    public TileData GetTile(int x, int y, int z = 0)
    {
        var data = new TileData();
        data.Type = _defaultType;
        data.Collision = _defaultType is TileType.Wall or TileType.Void ? TileCollision.Solid : TileCollision.Walkable;
        return data;
    }

    public void SetTile(int x, int y, int z, TileData tile)
    {
        // Stub — PrimitiveWorld is immutable beyond construction
    }

    public bool IsWalkable(Vec3I position)
    {
        var tile = GetTile(position.X, position.Y, position.Z);
        return tile.Collision == TileCollision.Walkable;
    }

    public bool CollidesWithSolid(Vec3I position)
    {
        var tile = GetTile(position.X, position.Y, position.Z);
        return tile.Collision == TileCollision.Solid;
    }

    public ICamera? Camera => null; // Stub — camera not needed for flat world

    public IEnumerable<TileDrawRegion> GetVisibleTiles()
    {
        // Return one region covering the entire world
        yield return new TileDrawRegion
        {
            Origin = new Vec2I(0, 0),
            Size = new Vec2I(_width, _height),
            Layer = 0
        };
    }
}
