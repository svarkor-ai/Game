using System.Collections.Generic;
using Djurspel.Core;
using Djurspel.Graphics;

namespace Djurspel.World;

/// <summary>Isometric world tile grid interface.</summary>
public interface IWorld
{
    int Width { get; }
    int Height { get; }
    int Layers { get; }
    TileData GetTile(int x, int y, int z = 0);
    void SetTile(int x, int y, int z, TileData tile);
    bool IsWalkable(Vec3I position);
    bool CollidesWithSolid(Vec3I position);
    ICamera? Camera { get; }
    IEnumerable<TileDrawRegion> GetVisibleTiles();
}
