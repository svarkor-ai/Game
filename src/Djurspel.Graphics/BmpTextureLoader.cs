using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using Djurspel.Core;
using System;
using System.IO;

namespace Djurspel.Graphics;

/// <summary>
/// Minimal BMP loader that creates an OpenGL texture from a BMP file.
/// Supports 24-bit and 32-bit BMP formats.
/// </summary>
public class BmpTextureLoader : ITextureLoader
{
    private readonly Dictionary<string, TextureAsset> _cache = new();

    public TextureAsset LoadTexture(string path)
    {
        if (_cache.TryGetValue(path, out var cached)) return cached;

       byte[] data;
        int width = 0, height = 0;
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(fs))
        {
            // Read BMP header
            var sig = br.ReadChar() + br.ReadChar();
            if (sig != 'B' + 'M') throw new InvalidDataException("Not a BMP file");
            br.ReadInt32(); // file size
            br.ReadInt16(); // reserved
            br.ReadInt16(); // reserved
            int dataOffset = br.ReadInt32(); // pixel data offset

            // Read DIB header
            br.ReadInt32(); // DIB header size
            width = br.ReadInt32();
            height = br.ReadInt32();
            short planes = br.ReadInt16();
            short bitsPerPixel = br.ReadInt16();
            br.ReadInt32(); // compression (0 = none)
            br.ReadInt32(); // image size
            br.ReadInt32(); // x pixels per meter
            br.ReadInt32(); // y pixels per meter
            br.ReadInt32(); // colors in color table
            br.ReadInt32(); // important colors

            if (planes != 1) throw new InvalidDataException($"Unsupported BMP planes: {planes}");
            if (bitsPerPixel != 24 && bitsPerPixel != 32)
                throw new InvalidDataException($"Unsupported BMP bits per pixel: {bitsPerPixel}");

            // Seek to pixel data
            fs.Seek(dataOffset, SeekOrigin.Begin);

            // BMP is stored bottom-up, so we reverse rows
            int rowSize = (width * (bitsPerPixel / 8) + 3) & ~3; // row size padded to 4 bytes
            byte[] rows = br.ReadBytes(rowSize * height);

            // Create pixel array (RGBA, top-down)
            int pixelSize = bitsPerPixel / 8;
            var pixels = new byte[width * height * 4];
            for (int row = 0; row < height; row++)
            {
                int srcRow = (height - 1 - row) * rowSize;
                for (int col = 0; col < width; col++)
                {
                    int srcIdx = srcRow + col * pixelSize;
                    int dstIdx = (row * width + col) * 4;
                    // BMP is BGR(A)
                    pixels[dstIdx] = (byte)rows[srcIdx];       // R
                    pixels[dstIdx + 1] = (byte)rows[srcIdx + 1];   // G
                    pixels[dstIdx + 2] = (byte)rows[srcIdx + 2];   // B
                    pixels[dstIdx + 3] = bitsPerPixel == 32 ? rows[srcIdx + 3] : (byte)255; // A
                }
            }

            data = pixels;
        }

        // Generate OpenGL texture
        int textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
          width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
          (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
          (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
          (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
          (int)TextureWrapMode.ClampToEdge);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        var asset = new TextureAsset
        {
            GlHandle = textureId,
            Width = width,
            Height = height,
            Format = TextureFormat.RGBA
        };

        _cache[path] = asset;
        return asset;
    }

    public void Dispose()
    {
        foreach (var texture in _cache.Values)
        {
            GL.DeleteTexture(texture.GlHandle);
        }
        _cache.Clear();
    }
}