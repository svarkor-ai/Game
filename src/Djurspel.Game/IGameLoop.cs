namespace Djurspel.Game;

/// <summary>Game loop controller — fixed timestep, frame pacing.</summary>
public interface IGameLoop : IDisposable
{
    void SetFixedTimestep(double fps);
    void Start();
    void Stop();
    void Frame();
}
