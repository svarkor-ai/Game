using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;

namespace Djurspel.WorldGen;

/// <summary>
/// Wilderness generator — generates a top-down 2D world with various terrain types.
/// Uses noise-based generation for natural-looking terrain.
/// </summary>
public class WildernessGenerator : IWorldGenerator
{
    private static readonly TileData TileGrass = new()
    {
        Type = TileType.Floor,
        Collision = TileCollision.Walkable,
        TintColor = new System.Numerics.Vector4(0.2f, 0.5f, 0.2f, 1.0f)
    };

    private static readonly TileData TileForest = new()
    {
        Type = TileType.Wall,
        Collision = TileCollision.Solid,
        TintColor = new System.Numerics.Vector4(0.1f, 0.3f, 0.1f, 1.0f)
    };

    private static readonly TileData TileWater = new()
    {
        Type = TileType.Water,
        Collision = TileCollision.Water,
        TintColor = new System.Numerics.Vector4(0.1f, 0.2f, 0.6f, 1.0f)
    };

    private static readonly TileData TileStone = new()
    {
        Type = TileType.Floor,
        Collision = TileCollision.Walkable,
        TintColor = new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1.0f)
    };

    private static readonly TileData TileSand = new()
    {
        Type = TileType.Floor,
        Collision = TileCollision.Walkable,
        TintColor = new System.Numerics.Vector4(0.8f, 0.7f, 0.3f, 1.0f)
    };

    private static readonly TileData TileVoid = new()
    {
        Type = TileType.Void,
        Collision = TileCollision.Solid,
        TintColor = new System.Numerics.Vector4(0.05f, 0.05f, 0.05f, 1.0f)
    };

    private readonly Random _rng;
    private readonly int _seed;

    public WildernessGenerator(int? seed = null)
    {
        _seed = seed ?? Random.Shared.Next();
        _rng = new Random(_seed);
    }

    public GeneratedLevel GenerateDungeon(int width, int height, int floor, Random? rng = null)
    {
        var dungeonGen = new RoomDungeonGenerator(rng ?? _rng);
        
        // Use RoomDungeonGenerator for dungeon generation
        var level = dungeonGen.Generate(
            minWidth: 5, minHeight: 5,
            maxWidth: 15, maxHeight: 20,
            minRooms: 3 + floor, maxRooms: 8 + floor,
            floor: floor);

        // Convert dungeon tiles to our tile system
        return ConvertDungeonLevel(level, width, height);
    }

    public GeneratedLevel GenerateWilderness(int width, int height, Random? rng = null)
    {
        var localRng = rng ?? _rng;
        var level = new GeneratedLevel();
        
        // Generate tile grid
        var grid = new TileData[width, height];
        
        // Initialize with void
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = TileVoid;
            }
        }

        // Generate terrain using simple noise
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float elevation = GetElevation(x, y, width, height);
                float moisture = GetMoisture(x, y, width, height);
                
                TileData tile;
                
                if (elevation < 0.2f)
                {
                    // Water
                    tile = TileWater;
                }
                else if (elevation < 0.25f)
                {
                    // Beach/sand
                    tile = TileSand;
                }
                else if (elevation < 0.7f)
                {
                    // Grassland
                    if (moisture > 0.6f && localRng.NextDouble() < 0.3f)
                    {
                        tile = TileForest; // Dense forest
                    }
                    else
                    {
                        tile = TileGrass;
                    }
                }
                else
                {
                    // Mountains/stone
                    tile = TileStone;
                }
                
                grid[x, y] = tile;
            }
        }

        // Find a valid starting position (grassy area)
        Vec3I playerStart = FindPlayerStart(grid, width, height);
        
        // Generate enemy and loot spawns
        var enemies = GenerateSpawns(grid, width, height, "Enemy", localRng, count: 10 + _seed % 5);
        var loot = GenerateSpawns(grid, width, height, "Loot", localRng, count: 5 + _seed % 3);

        return new GeneratedLevel
        {
            Tiles = grid,
            EnemySpawns = enemies,
            LootSpawns = loot,
            PlayerStart = playerStart,
            Name = $"Wilderness (Seed: {_seed})"
        };
    }

    private GeneratedLevel ConvertDungeonLevel(GeneratedLevel dungeon, int width, int height)
    {
        var grid = new TileData[width, height];
        
        // Initialize with void
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = TileVoid;
            }
        }

        // Copy dungeon tiles
        int dw = dungeon.Tiles.GetLength(0);
        int dh = dungeon.Tiles.GetLength(1);
        
        for (int x = 0; x < Math.Min(width, dw); x++)
        {
            for (int y = 0; y < Math.Min(height, dh); y++)
            {
                var dungeonTile = dungeon.Tiles[x, y];
                TileData tile = dungeonTile.Type switch
                {
                    TileType.Wall => TileForest,
                    TileType.Floor => TileGrass,
                    TileType.Water => TileWater,
                    TileType.Door => TileGrass, // Treat doors as walkable
                    _ => TileVoid
                };
                grid[x, y] = tile;
            }
        }

        return new GeneratedLevel
        {
            Tiles = grid,
            EnemySpawns = dungeon.EnemySpawns,
            LootSpawns = dungeon.LootSpawns,
            PlayerStart = dungeon.PlayerStart,
            Name = dungeon.Name
        };
    }

    private float GetElevation(int x, int y, int width, int height)
    {
        // Simple noise using sine waves
        float nx = (float)x / width;
        float ny = (float)y / height;
        
        float e = 0.5f;
        e += 0.3f * MathF.Sin(nx * 3.14159f * 2.0f);
        e += 0.2f * MathF.Cos(ny * 3.14159f * 3.0f);
        e += 0.1f * MathF.Sin((nx + ny) * 3.14159f * 4.0f);
        
        return MathF.Min(MathF.Max(e, 0f), 1f);
    }

    private float GetMoisture(int x, int y, int width, int height)
    {
        float nx = (float)x / width;
        float ny = (float)y / height;
        
        float m = 0.5f;
        m += 0.3f * MathF.Cos(nx * 3.14159f * 2.5f);
        m += 0.2f * MathF.Sin((ny * 2.0f + nx) * 3.14159f);
        
        return MathF.Min(MathF.Max(m, 0f), 1f);
    }

    private Vec3I FindPlayerStart(TileData[,] grid, int width, int height)
    {
        // Find a floor tile (walkable, not wall)
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (grid[x, y].Type == TileType.Floor && grid[x, y].Collision != TileCollision.Solid)
                {
                    return new Vec3I(x, y, 0);
                }
            }
        }
        
        // Fallback to center
        return new Vec3I(width / 2, height / 2, 0);
    }

    private EntityDefinition[] GenerateSpawns(TileData[,] grid, int width, int height, string type, Random rng, int count)
    {
        var spawns = new List<EntityDefinition>();
        
        for (int i = 0; i < count; i++)
        {
            // Find a random walkable position
            int x, y;
            do
            {
                x = rng.Next(1, width - 1);
                y = rng.Next(1, height - 1);
            } while (grid[x, y].Type == TileType.Wall || grid[x, y].Collision == TileCollision.Solid);
            
            spawns.Add(new EntityDefinition
            {
                Type = type,
                Name = $"{type}_{i}"
            });
            spawns[^1].ComponentData["position"] = new Vec3I(x, y, 0);
        }
        
        return spawns.ToArray();
    }
}
