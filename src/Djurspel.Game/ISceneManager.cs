using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;

namespace Djurspel.Game;

/// <summary>Scene manager — loads/unloads scenes (levels).</summary>
public interface ISceneManager
{
    void LoadScene(string scenePath);
    void UnloadScene();
    IWorld? World { get; }
    Entity? PlayerEntity { get; }
}
