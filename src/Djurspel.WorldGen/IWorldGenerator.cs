using System;
using System.Collections.Generic;
using Djurspel.World;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.WorldGen;

namespace Djurspel.WorldGen;

/// <summary>Interface for world generators. Implement this to define custom world generation algorithms.</summary>
public interface IWorldGenerator
{
    /// <summary>Generate a dungeon level with the specified dimensions and floor.</summary>
    GeneratedLevel GenerateDungeon(int width, int height, int floor, Random? rng = null);

    /// <summary>Generate a wilderness map with the specified dimensions.</summary>
    GeneratedLevel GenerateWilderness(int width, int height, Random? rng = null);
}
