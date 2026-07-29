using System;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;

namespace Djurspel.WorldGen;

/// <summary>
/// Generates outdoor wilderness areas using Perlin noise for terrain variation.
/// Creates ground, water, and path tiles based on noise thresholds.
/// </summary>
public class WildernessGenerator : IWorldGenerator
{
    private readonly Random _rng;
    private readonly SimplexNoise _noise;

    public WildernessGenerator(Random? rng = null)
    {
        _rng = rng ?? new Random();
        _noise = new SimplexNoise(_rng.Next());
    }

    public GeneratedLevel GenerateDungeon(int width, int height, int floor, Random? rng = null)
    {
        // Delegate to RoomDungeonGenerator for dungeon generation
        var roomGen = new RoomDungeonGenerator(rng);
        
        // Calculate room parameters based on floor
        int minWidth = 4;
        int minHeight = 4;
        int maxWidth = Math.Min(12, width / 4);
        int maxHeight = Math.Min(8, height / 4);
        int minRooms = Math.Max(3, (width * height) / 300);
        int maxRooms = Math.Min(20, (width * height) / 200);

        return roomGen.Generate(minWidth, minHeight, maxWidth, maxHeight, minRooms, maxRooms, floor);
    }

    public GeneratedLevel GenerateWilderness(int width, int height, Random? rng = null)
    {
        Random localRng = rng ?? _rng;
        SimplexNoise localNoise = new SimplexNoise(localRng.Next());
        
        // Generate tile grid
        TileData[,] grid = new TileData[width, height];
        
        // Noise scale and thresholds
        double scale = 0.05;
        double waterThreshold = 0.1;
        double pathThreshold = 0.3;
        
        // Fill grid with noise values
        double[,] noiseValues = new double[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                double nx = x * scale;
                double ny = y * scale;
                noiseValues[x, y] = localNoise.GetNoise2D(nx, ny);
            }
        }
        
        // Convert noise values to tiles
        int playerX = width / 2;
        int playerY = height / 2;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                double value = noiseValues[x, y];
                TileData tile;
                
                if (value < waterThreshold)
                {
                    tile = new TileData
                    {
                        Type = TileType.Water,
                        Collision = TileCollision.Water,
                        TintColor = new(0.2f, 0.3f, 0.6f, 1f)
                    };
                }
                else if (value < pathThreshold)
                {
                    tile = new TileData
                    {
                        Type = TileType.Ground,
                        Collision = TileCollision.Walkable,
                        TintColor = new(0.4f, 0.5f, 0.3f, 1f)
                    };
                }
                else
                {
                    // Higher ground - maybe walls or decorative features
                    tile = new TileData
                    {
                        Type = TileType.Ground,
                        Collision = TileCollision.Walkable,
                        TintColor = new(0.3f, 0.4f, 0.25f, 1f)
                    };
                }
                
                // Ensure player start is on walkable ground
                if (x == playerX && y == playerY)
                {
                    tile = new TileData
                    {
                        Type = TileType.Ground,
                        Collision = TileCollision.Walkable,
                        TintColor = new(0.6f, 0.55f, 0.5f, 1f)
                    };
                }
                
                grid[x, y] = tile;
            }
        }
        
        // Generate spawns
        var enemies = GenerateEnemySpawns(grid, width, height, localRng);
        var loot = GenerateLootSpawns(grid, width, height, localRng);
        
        return new GeneratedLevel
        {
            Tiles = grid,
            EnemySpawns = enemies,
            LootSpawns = loot,
            PlayerStart = new Vec3I(playerX, playerY, 0),
            Name = "Wilderness Area"
        };
    }
    
    private EntityDefinition[] GenerateEnemySpawns(TileData[,] grid, int width, int height, Random rng)
    {
        var enemies = new List<EntityDefinition>();
        int enemyCount = (width * height) / 400; // 1 enemy per 400 tiles
        
        for (int i = 0; i < enemyCount; i++)
        {
            // Find a walkable tile for enemy
            int ex, ey;
            int attempts = 0;
            do
            {
                ex = rng.Next(width);
                ey = rng.Next(height);
                attempts++;
            } while ((grid[ex, ey].Type != TileType.Ground || grid[ex, ey].Collision == TileCollision.Water) && attempts < 100);
            
            if (attempts < 100)
            {
                enemies.Add(new EntityDefinition
                {
                    Type = "WildernessEnemy",
                    Name = $"WildernessEnemy_{i}",
                    ComponentData =
                    {
                        ["position"] = new Vec3I(ex, ey, 0)
                    }
                });
            }
        }
        
        return enemies.ToArray();
    }
    
    private EntityDefinition[] GenerateLootSpawns(TileData[,] grid, int width, int height, Random rng)
    {
        var loot = new List<EntityDefinition>();
        int lootCount = (width * height) / 300; // 1 loot per 300 tiles
        
        for (int i = 0; i < lootCount; i++)
        {
            // Find a walkable tile for loot
            int lx, ly;
            int attempts = 0;
            do
            {
                lx = rng.Next(width);
                ly = rng.Next(height);
                attempts++;
            } while ((grid[lx, ly].Type != TileType.Ground || grid[lx, ly].Collision == TileCollision.Water) && attempts < 100);
            
            if (attempts < 100)
            {
                loot.Add(new EntityDefinition
                {
                    Type = "WildernessLoot",
                    Name = $"WildernessLoot_{i}",
                    ComponentData =
                    {
                        ["position"] = new Vec3I(lx, ly, 0)
                    }
                });
            }
        }
        
        return loot.ToArray();
    }
}
