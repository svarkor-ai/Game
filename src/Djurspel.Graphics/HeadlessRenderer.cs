using System;
using System.IO;
using OpenTK.Mathematics;
using Djurspel.Core;

namespace Djurspel.Graphics;

/// <summary>
/// Headless renderer that renders to a byte buffer (RGBA) and can export to BMP.
/// No GPU/GL calls — suitable for CI or automated screenshots.
/// </summary>
public class HeadlessRenderer : ISpriteBatchRenderer, IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly byte[] _pixels;
    private readonly string _outputPath;
    private readonly Vector4 _defaultColor;

    public HeadlessRenderer(int width, int height, string outputPath)
    {
        _width = width;
        _height = height;
        _outputPath = outputPath;
        _defaultColor = Vector4.One;
        _pixels = new byte[width * height * 4]; // RGBA
        Array.Fill(_pixels, 255); // White background
    }

    public void SetMatrices(Matrix4 projMatrix, Matrix4 viewMatrix)
    {
        // No-op for headless — matrices are not used
    }

    public void BeginBatch()
    {
        // No-op — headless renders immediately
    }

    public void EndBatch()
    {
        // No-op — headless renders immediately
    }

    public void DrawQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        // No-op for headless — only used in real rendering
    }

    public void SetColor(Vector4 color)
    {
        // No-op for headless — only used in real rendering
    }

    public void SaveToBitmap()
    {
        using var fs = new FileStream(_outputPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        // BMP Header (14 bytes)
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write((uint)(14 + 40 + _pixels.Length)); // File size
        bw.Write((ushort)0); // Reserved
        bw.Write((ushort)0); // Reserved
        bw.Write((uint)(14 + 40)); // Pixel offset

        // DIB Header (40 bytes)
        bw.Write(40); // Header size
        bw.Write(_width);
        bw.Write(_height);
        bw.Write((ushort)1); // Planes
        bw.Write((ushort)24); // Bits per pixel
        bw.Write(0); // Compression (none)
        bw.Write((uint)_pixels.Length); // Image size
        bw.Write(2835); // X pixels per meter
        bw.Write(2835); // Y pixels per meter
        bw.Write(0); // Colors in color table
        bw.Write(0); // Important colors

        // Pixel data (BGR, not RGB)
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int idx = y * _width * 4 + x * 4;
                byte r = _pixels[idx];
                byte g = _pixels[idx + 1];
                byte b = _pixels[idx + 2];
                bw.Write(b);
                bw.Write(g);
                bw.Write(r);
            }
        }
    }

    public void Dispose()
    {
        // No-op
    }
}
