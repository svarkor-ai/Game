// Minimal test: OpenTK GameWindow that just clears to blue
// NOTE: This file is kept separate from the main solution to avoid
// duplicate entry point conflicts. Build it directly:
//   dotnet build minimal-test.csproj
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System;

class MinimalTestProgram
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

        window.Load += delegate
        {
            Console.WriteLine("=== OPENGL LOADED ===");
            var version = GL.GetString(StringName.Version);
            Console.WriteLine($"OpenGL version: {version}");
            GL.ClearColor(0f, 0f, 1f, 1f); // Blue
        };

        window.RenderFrame += (e) =>
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            window.SwapBuffers();
            Console.WriteLine("=== FRAME RENDERED ===");
        };

        // Run only 10 frames then exit (headless-friendly)
        int frames = 0;
        window.RenderFrame += (e) =>
        {
            frames++;
            Console.WriteLine($"Frame {frames}");
            if (frames >= 10)
            {
                Console.WriteLine("=== DONE — exiting after 10 frames ===");
                window.Close();
            }
        };

        window.Run();
    }
}
