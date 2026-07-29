using System;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;

namespace Djurspel.WorldGen;

/// <summary>
/// Room-based dungeon generator. Places rectangular rooms and connects them with corridors.
/// Uses a simple BSP-inspired approach: place rooms randomly, then connect them in a spanning tree.
/// </summary>
public class RoomDungeonGenerator : IRoomDungeonGenerator
{
    private static readonly TileData TileWall = new()
    {
        Type = TileType.Wall,
        Collision = TileCollision.Solid,
        TintColor = new(0.3f, 0.3f, 0.3f, 1f)
    };

    private static readonly TileData TileFloor = new()
    {
        Type = TileType.Floor,
        Collision = TileCollision.Walkable,
        TintColor = new(0.6f, 0.55f, 0.5f, 1f)
    };

    private static readonly TileData TileDoor = new()
    {
        Type = TileType.Door,
        Collision = TileCollision.Interactable,
        TintColor = new(0.4f, 0.3f, 0.2f, 1f)
    };

    private readonly Random _rng = new(42); // Default seed

    public RoomDungeonGenerator(Random? rng = null)
    {
        _rng = rng ?? new Random();
    }

    public GeneratedLevel Generate(
        int minWidth, int minHeight,
        int maxWidth, int maxHeight,
        int minRooms, int maxRooms,
        int floor)
    {
        int width = 80;
        int height = 60;
        int roomCount = _rng.Next(minRooms, maxRooms + 1);

        // Step 1: Generate tile grid filled with walls
        TileData[,] grid = new TileData[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = TileWall;
            }
        }

        // Step 2: Place rooms
        var rooms = new List<Rect>(roomCount);
        for (int attempt = 0; attempt < 200 && rooms.Count < roomCount; attempt++)
        {
            int rw = _rng.Next(minWidth, maxWidth + 1);
            int rh = _rng.Next(minHeight, maxHeight + 1);
            int rx = _rng.Next(1, width - rw - 1);
            int ry = _rng.Next(1, height - rh - 1);

            Rect room = new Rect(rx, ry, rw, rh);

            // Check overlap with existing rooms (with 1-tile gap)
            bool overlaps = false;
            foreach (var existing in rooms)
            {
                if (room.OverlapsWithGap(existing, 1))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                rooms.Add(room);
                CarveRoom(grid, room);
            }
        }

        // Step 3: Connect rooms with a spanning tree (minimum connected graph)
        if (rooms.Count > 1)
        {
            ConnectRooms(grid, rooms);
        }

        // Step 4: Add doors between rooms
        AddDoors(grid, rooms);

        // Step 5: Find player start (center of first room)
        Vec3I playerStart = Vec3I.Zero;
        if (rooms.Count > 0)
        {
            Rect firstRoom = rooms[0];
            playerStart = new Vec3I(
                firstRoom.X + firstRoom.W / 2,
                firstRoom.Y + firstRoom.H / 2,
                0);
        }

        // Step 6: Generate spawns
        var enemies = GenerateEnemySpawns(rooms, floor, roomCount);
        var loot = GenerateLootSpawns(rooms, floor, roomCount);

        return new GeneratedLevel
        {
            Tiles = grid,
            EnemySpawns = enemies,
            LootSpawns = loot,
            PlayerStart = playerStart,
            Name = $"Dungeon Floor {floor}"
        };
    }

    /// <summary>Carve a rectangular room into the tile grid (set to floor tiles).</summary>
    private static void CarveRoom(TileData[,] grid, Rect room)
    {
        for (int x = room.X; x < room.X + room.W; x++)
        {
            for (int y = room.Y; y < room.Y + room.H; y++)
            {
                if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                {
                    grid[x, y] = TileFloor;
                }
            }
        }
    }

    /// <summary>Connect rooms using a minimum spanning tree (Prim-like approach).</summary>
    private static void ConnectRooms(TileData[,] grid, List<Rect> rooms)
    {
        var visited = new HashSet<int> { 0 };
        var edges = new List<(int from, int to, Point a, Point b)>();

        // Find nearest unvisited room to any visited room, repeat
        while (visited.Count < rooms.Count)
        {
            int bestFrom = -1;
            int bestTo = -1;
            int bestDist = int.MaxValue;
            Point bestA = Point.Zero;
            Point bestB = Point.Zero;

            foreach (int vi in visited)
            {
                Rect roomA = rooms[vi];
                Point centerA = new Point(roomA.X + roomA.W / 2, roomA.Y + roomA.H / 2);

                for (int uj = 0; uj < rooms.Count; uj++)
                {
                    if (visited.Contains(uj)) continue;
                    Rect roomB = rooms[uj];
                    Point centerB = new Point(roomB.X + roomB.W / 2, roomB.Y + roomB.H / 2);
                    int dist = Math.Abs(centerA.X - centerB.X) + Math.Abs(centerA.Y - centerB.Y);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFrom = vi;
                        bestTo = uj;
                        bestA = centerA;
                        bestB = centerB;
                    }
                }
            }

            if (bestFrom >= 0 && bestTo >= 0)
            {
                visited.Add(bestTo);
                edges.Add((bestFrom, bestTo, bestA, bestB));
            }
        }

        // Carve corridors for each edge
        foreach (var edge in edges)
        {
            CarveCorridor(grid, edge.a, edge.b);
        }
    }

    /// <summary>Carve an L-shaped corridor between two points.</summary>
    private static void CarveCorridor(TileData[,] grid, Point a, Point b)
    {
        // Horizontal then vertical
        int x = a.X;
        int y = a.Y;

        // Move horizontally
        int dx = Math.Sign(b.X - a.X);
        while (x != b.X)
        {
            if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
            {
                if (grid[x, y].Type == TileType.Wall)
                    grid[x, y] = TileFloor;
            }
            x += dx;
        }

        // Move vertically
        int dy = Math.Sign(b.Y - a.Y);
        while (y != b.Y)
        {
            if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
            {
                if (grid[x, y].Type == TileType.Wall)
                    grid[x, y] = TileFloor;
            }
            y += dy;
        }
    }

    /// <summary>Add door tiles at room boundaries.</summary>
    private static void AddDoors(TileData[,] grid, List<Rect> rooms)
    {
        foreach (var room in rooms)
        {
            // Check each edge for adjacency to another room
            int cx = room.X + room.W / 2;
            int cy = room.Y + room.H / 2;

            // Top edge
            if (cy > 0 && grid[cx, cy - 1].Type == TileType.Floor && room.Y > 0)
            {
                grid[cx, cy - 1] = TileDoor;
            }

            // Bottom edge
            if (cy < grid.GetLength(1) - 1 && grid[cx, cy + 1].Type == TileType.Floor)
            {
                grid[cx, cy + 1] = TileDoor;
            }

            // Left edge
            if (cx > 0 && grid[cx - 1, cy].Type == TileType.Floor && room.X > 0)
            {
                grid[cx - 1, cy] = TileDoor;
            }

            // Right edge
            if (cx < grid.GetLength(0) - 1 && grid[cx + 1, cy].Type == TileType.Floor)
            {
                grid[cx + 1, cy] = TileDoor;
            }
        }
    }

    /// <summary>Generate enemy spawns based on floor level and room count.</summary>
    private EntityDefinition[] GenerateEnemySpawns(List<Rect> rooms, int floor, int roomCount)
    {
        var enemies = new List<EntityDefinition>();
        int enemyCount = roomCount * (floor + 1) / 2;

        for (int i = 0; i < enemyCount; i++)
        {
            Rect room = rooms[i % rooms.Count];
            int ex = room.X + _rng.Next(1, room.W - 1);
            int ey = room.Y + _rng.Next(1, room.H - 1);

            enemies.Add(new EntityDefinition
            {
                Type = "Enemy",
                Name = $"Enemy_{floor}_{i}",
                ComponentData =
                {
                    ["position"] = new Vec3I(ex, ey, 0),
                    ["floor"] = floor
                }
            });
        }

        return enemies.ToArray();
    }

    /// <summary>Generate loot spawns based on floor level.</summary>
    private EntityDefinition[] GenerateLootSpawns(List<Rect> rooms, int floor, int roomCount)
    {
        var loot = new List<EntityDefinition>();
        int lootCount = roomCount * 2;

        for (int i = 0; i < lootCount; i++)
        {
            Rect room = rooms[i % rooms.Count];
            int lx = room.X + _rng.Next(1, room.W - 1);
            int ly = room.Y + _rng.Next(1, room.H - 1);

            loot.Add(new EntityDefinition
            {
                Type = "Loot",
                Name = $"Loot_{floor}_{i}",
                ComponentData =
                {
                    ["position"] = new Vec3I(lx, ly, 0),
                    ["floor"] = floor
                }
            });
        }

        return loot.ToArray();
    }

    /// <summary>Simple rectangle with overlap detection.</summary>
    private struct Rect
    {
        public int X, Y, W, H;

        public Rect(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }

        public bool OverlapsWithRect(Rect other)
        {
            return X < other.X + other.W && X + W > other.X &&
                   Y < other.Y + other.H && Y + H > other.Y;
        }

        public bool OverlapsWithGap(Rect other, int gap)
        {
            return X - gap < other.X + other.W && X + W + gap > other.X &&
                   Y - gap < other.Y + other.H && Y + H + gap > other.Y;
        }
    }

    /// <summary>2D integer point.</summary>
    private struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
        public static Point Zero => new Point(0, 0);
    }
}
