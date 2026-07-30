// Minimal test: OpenTK GameWindow that just clears to blue
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

class Program
{
    static void Main()
    {
        using var window = new GameWindow(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                Title = "Minimal Test",
                ClientSize = new Vector2i(1280, 720),
                StartVisible = false,
            });

        window.Load += (s, e) =>
        {
            Console.WriteLine("=== OPENGL LOADED ===");
            var version = GL.GetString(StringName.Version);
            Console.WriteLine($"OpenGL version: {version}");
            GL.ClearColor(0f, 0f, 1f, 1f); // Blue
        };

        window.RenderFrame += (s, e) =>
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            window.SwapBuffers();
            Console.WriteLine("=== FRAME RENDERED ===");
        };

        window.Run();
    }
}
