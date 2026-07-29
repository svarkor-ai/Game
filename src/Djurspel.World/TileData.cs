using Djurspel.Core;
using Djurspel.Graphics;

namespace Djurspel.World;

/// <summary>Tile type enumeration.</summary>
public enum TileType
{
    Ground = 0,
    Wall = 1,
    Floor = 2,
    Door = 3,
    Water = 4,
    Stairs = 5,
    Void = 6
}

/// <summary>Collision mask for a tile.</summary>
public enum TileCollision
{
    None = 0,
    Walkable = 1,
    Solid = 2,
    Water = 4,
    Interactable = 8
}

/// <summary>A single tile in the isometric grid.</summary>
public struct TileData
{
    public TileType Type;
    public TileCollision Collision;
    public string? MeshPath;
    public string? TexturePath;
    public System.Numerics.Vector4 TintColor;
    public float HeightOffset;
}

/// <summary>A region of tiles to draw together.</summary>
public struct TileDrawRegion
{
    public Core.Vec2I Origin;
    public Core.Vec2I Size;
    public int Layer;
}
