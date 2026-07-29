using OpenTK.Mathematics;

namespace Djurspel.Graphics;

/// <summary>
/// Minimal game window interface — maps to OpenTK 4 NativeWindow capabilities.
/// </summary>
public interface IGameWindow
{
    int Width { get; }
    int Height { get; }
    bool IsOpen { get; }
    bool ShouldClose { get; }
    void SwapBuffers();
    void Close();
    void SetTitle(string title);
    void ProcessEvents();
    Vector2 MousePosition { get; }
    bool IsMouseButtonPressed(int button);
    bool IsKeyDown(int key);
}
