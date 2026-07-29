using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;

namespace Djurspel.WorldGen;

/// <summary>Result of a world generation pass. Contains the tile grid and spawn data for a generated level.</summary>
public struct GeneratedLevel
{
    /// <summary>The 2D tile grid (width x height).</summary>
    public TileData[,] Tiles;

    /// <summary>Enemy spawn points within this level.</summary>
    public EntityDefinition[] EnemySpawns;

    /// <summary>Loot/spawn points within this level.</summary>
    public EntityDefinition[] LootSpawns;

    /// <summary>Starting position for the player character.</summary>
    public Vec3I PlayerStart;

    /// <summary>Human-readable name for this level.</summary>
    public string Name { get; set; }

    public GeneratedLevel()
    {
        Tiles = new TileData[0, 0];
        EnemySpawns = Array.Empty<EntityDefinition>();
        LootSpawns = Array.Empty<EntityDefinition>();
        PlayerStart = Vec3I.Zero;
        Name = "";
    }
}
