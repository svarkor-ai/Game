namespace Djurspel.Game;

/// <summary>Game loop controller — fixed timestep, interpolation, frame pacing.</summary>
public interface IGameLoop : IDisposable
{
    void SetFixedTimestep(double fps);
    void RegisterUpdate(System.Action<double> update, string name);
    void RegisterRender(System.Action<double> render, string name);
    void Start();
    void Stop();
}
