namespace Djurspel.World;

/// <summary>World factory — creates worlds from definitions.</summary>
public interface IWorldFactory
{
    IWorld CreateFromJson(string levelJsonPath);
    IWorld CreateFromPrimitive(int width, int height, int layers, TileType defaultType);
}
