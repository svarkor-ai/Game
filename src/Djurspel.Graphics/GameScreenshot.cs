using OpenTK.Windowing.Common;
using OSG = OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Djurspel.Graphics;

/// <summary>
/// Screenshot-funktioner för GameWindow.
/// Hanterar framebuffer-läsning och BMP-export.
/// </summary>
public class GameScreenshot
{
    private readonly OSG.GameWindow _gameWindow;
    private int _renderFrameCount = 0;
    private string? _screenshotPath = null;
    private bool _screenshotTaken = false;

    public GameScreenshot(OSG.GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
    }

    public void SetScreenshotPath(string path)
    {
        _screenshotPath = path;
    }

    public bool ScreenshotTaken => _screenshotTaken;

    public void OnRenderFrame(FrameEventArgs e)
    {
        if (_screenshotPath == null || _screenshotTaken)
            return;

        _renderFrameCount++;
        if (_renderFrameCount == 10)
        {
            CaptureFramebufferToPng(_screenshotPath);
            Console.Error.WriteLine("[GameWindow] Screenshot captured, now swapping...");
            SwapBuffers();
            Console.Error.WriteLine("[GameWindow] Screenshot saved to " + _screenshotPath);
            _screenshotPath = null;
            _screenshotTaken = true;
            _gameWindow.Close();
        }
        else
        {
            SwapBuffers();
        }
    }

    private void SwapBuffers()
    {
        _gameWindow.SwapBuffers();
    }

    /// <summary>Read OpenGL back framebuffer and save as BMP (no external tools needed).</summary>
    private void CaptureFramebufferToPng(string outputPath)
    {
        try
        {
            int w = _gameWindow.Size.X;
            int h = _gameWindow.Size.Y;
            int rowSize = w * 3;
            int paddedRowSize = (rowSize + 3) & ~3;
            int imageSize = paddedRowSize * h;
            int fileSize = 54 + imageSize;

            Console.Error.WriteLine($"[GameWindow] CaptureFramebufferToPng: {w}x{h}, imageSize={imageSize}, path={outputPath}");

            OSG.GL.ReadBuffer(OSG.ReadBufferMode.Back);
            byte[] pixels = new byte[imageSize];
            OSG.GL.ReadPixels(0, 0, w, h, OSG.PixelFormat.Rgb, OSG.PixelType.UnsignedByte, pixels);
            
            OSG.GL.Finish();
            int err;
            List<string> errors = new();
            while ((err = (int)OSG.GL.GetError()) != 0)
            {
                errors.Add($"GL_ERROR: {err}");
            }
            if (errors.Count > 0)
                Console.Error.WriteLine("[GameWindow] GL errors during capture: " + string.Join(", ", errors));
            
            Console.Error.WriteLine($"[GameWindow] ReadPixels: read {pixels.Length} bytes, first 10: {string.Join(",", pixels.Take(10))}");

            var uniqueColors = new HashSet<string>();
            int sampledCount = 0;
            int zeroCount = 0;
            for (int i = 0; i < pixels.Length; i += 3)
            {
                int r = pixels[i], g = pixels[i + 1], b = pixels[i + 2];
                if (r == 0 && g == 0 && b == 0) zeroCount++;
                uniqueColors.Add($"{r},{g},{b}");
                sampledCount++;
            }
            Console.Error.WriteLine($"[GameWindow] ReadPixels analysis: sampled={sampledCount}, zeros={zeroCount}, unique_colors={uniqueColors.Count}");
            var topColors = uniqueColors.Take(20).ToList();
            Console.Error.WriteLine($"[GameWindow] Top colors: {string.Join(" | ", topColors)}");

            byte[] flipRow = new byte[paddedRowSize];
            for (int y = 0; y < h / 2; y++)
            {
                int top = y * paddedRowSize;
                int bottom = (h - 1 - y) * paddedRowSize;
                Array.Copy(pixels, top, flipRow, 0, paddedRowSize);
                Array.Copy(pixels, bottom, pixels, top, paddedRowSize);
                Array.Copy(flipRow, 0, pixels, bottom, paddedRowSize);
            }

            byte[] paddedPixels = new byte[imageSize];
            for (int y = 0; y < h; y++)
            {
                Array.Copy(pixels, y * rowSize, paddedPixels, y * paddedRowSize, rowSize);
            }

            using var fs = File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            bw.Write((byte)'B');
            bw.Write((byte)'M');
            bw.Write(fileSize);
            bw.Write((short)0);
            bw.Write((short)0);
            bw.Write(54);
            bw.Write(40);
            bw.Write(w);
            bw.Write(h);
            bw.Write((short)1);
            bw.Write((short)24);
            bw.Write(0);
            bw.Write(imageSize);
            bw.Write(2835);
            bw.Write(2835);
            bw.Write(0);
            bw.Write(0);

            Console.Error.WriteLine("[GameWindow] BMP header written. Writing pixels...");
            bw.Write(paddedPixels);
            bw.Flush();
            Console.Error.WriteLine("[GameWindow] BMP file written successfully: " + fileSize + " bytes");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[GameWindow] ERROR in CaptureFramebufferToPng: " + ex.ToString());
        }
    }

    public void TakeHeadlessScreenshot(string outputPath, int frames = 10)
    {
        SetScreenshotPath(outputPath);
        
        for (int i = 0; i < frames; i++)
        {
            var updateCB = _gameWindow.UpdateFrameCallback;
            if (updateCB != null)
                updateCB(1.0 / 60.0);
            
            OnRenderFrame(new FrameEventArgs(1.0 / 60.0));
        }
        
        Console.Error.WriteLine("[GameWindow] Headless screenshot done, frames=" + frames);
    }
}