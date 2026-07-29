using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.Graphics;

namespace Djurspel.World;

/// <summary>
/// Stub implementation of IWorld — a simple 2D tile grid with Z-layer support.
/// Used during prototyping until procedural generation is implemented.
/// </summary>
public class TileMap : IWorld
{
    private readonly TileData[,,] _tiles;
    private readonly ICamera _camera;
    private readonly IEventDispatcher? _dispatcher;

    public int Width { get; }
    public int Height { get; }
    public int Layers { get; }

    public TileMap(int width, int height, int layers, TileType defaultType, ICamera camera, IEventDispatcher? dispatcher = null)
    {
        Width = width;
        Height = height;
        Layers = layers;
        _camera = camera;
        _dispatcher = dispatcher;

        _tiles = new TileData[width, height, layers];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < layers; z++)
                    _tiles[x, y, z] = MakeDefaultTile(defaultType);
    }

    private static TileData MakeDefaultTile(TileType type)
    {
        TileCollision col = type == TileType.Wall ? TileCollision.Solid :
                            type == TileType.Water ? TileCollision.Water :
                            type == TileType.Void ? TileCollision.None : TileCollision.Walkable;
        return new TileData
        {
            Type = type,
            Collision = col,
            TintColor = new System.Numerics.Vector4(Core.Color.White.ToFloatArray()[0], Core.Color.White.ToFloatArray()[1], Core.Color.White.ToFloatArray()[2], Core.Color.White.ToFloatArray()[3]),
            HeightOffset = 0f
        };
    }

    public TileData GetTile(int x, int y, int z = 0)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Layers)
            return new TileData { Type = TileType.Void };
        return _tiles[x, y, z];
    }

    public void SetTile(int x, int y, int z, TileData tile)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Layers)
            return;
        _tiles[x, y, z] = tile;
        _dispatcher?.Dispatch(new TileChangedEvent(x, y, z));
    }

    public bool IsWalkable(Core.Vec3I position)
    {
        var t = GetTile(position.X, position.Y, position.Z);
        return t.Type != TileType.Void && (t.Collision & TileCollision.Solid) == 0;
    }

    public bool CollidesWithSolid(Core.Vec3I position)
    {
        var t = GetTile(position.X, position.Y, position.Z);
        return t.Type != TileType.Void && (t.Collision & TileCollision.Solid) != 0;
    }

    public ICamera Camera => _camera;

    public IEnumerable<TileDrawRegion> GetVisibleTiles()
    {
        // Return the entire grid for now — frustum culling comes later.
        yield return new TileDrawRegion
        {
            Origin = new Core.Vec2I(0, 0),
            Size = new Core.Vec2I(Width, Height),
            Layer = 0
        };
    }
}

// Event type for tile changes
public record TileChangedEvent(int X, int Y, int Z) : IEvent;
