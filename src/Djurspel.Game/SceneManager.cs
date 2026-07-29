using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.World;

namespace Djurspel.Game;

/// <summary>
/// Stub implementation of ISceneManager — manages game scenes (levels).
/// Full implementation will load/parse scene files and manage entities per level.
/// </summary>
public class SceneManager : ISceneManager
{
    private IWorld? _world;
    private Entity? _playerEntity;

    public IWorld? World => _world;
    public Entity? PlayerEntity => _playerEntity;

    public void LoadScene(string scenePath)
    {
        // Stub — in full implementation, this would parse the scene file,
        // create the world, load entities, and set up the player.
        _world = null;
        _playerEntity = null;
    }

    public void UnloadScene()
    {
        _world = null;
        _playerEntity = null;
    }
}
