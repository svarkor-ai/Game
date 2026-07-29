using System;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;
using Djurspel.WorldGen;

namespace Djurspel.WorldGen;

/// <summary>Interface for room-based dungeon generation. Generates structured dungeons with rooms and corridors.</summary>
public interface IRoomDungeonGenerator
{
    /// <summary>Generate a room-based dungeon with the specified parameters.</summary>
    GeneratedLevel Generate(
        int minWidth, int minHeight,
        int maxWidth, int maxHeight,
        int minRooms, int maxRooms,
        int floor);
}
