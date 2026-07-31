using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using Djurspel.Core;
using System.Runtime.CompilerServices;

namespace Djurspel.Graphics;

/// <summary>
/// 2D SpriteBatch Renderer som batchar sprite draws för effektiv rendering.
/// Använder vertex buffer för att rendera 2D sprites med färger och transformationer.
/// </summary>
public class SpriteBatchRenderer : IDisposable
{
    private readonly int _vertexBuffer;
    private readonly int _indexBuffer;
    private readonly int _shaderProgram;
    
    private const int MaxVertices = 65536; // 64K vertices per batch
    private const int MaxIndices = MaxVertices / 2; // 32K indices (quads)
    
    private readonly float[] _vertices; // xyz, uv, color (9 floats per vertex)
    private readonly ushort[] _indices;
    private int _vertexCount;
    private int _indexCount;
    private bool _batchStarted;
    private Vector4 _currentColor = Vector4.One;

    public SpriteBatchRenderer()
    {
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        _vertices = new float[MaxVertices * 9];
        _indices = new ushort[MaxIndices];
        _vertexCount = 0;
        _indexCount = 0;
        _batchStarted = false;
        
        // Create a simple 2D shader program
        _shaderProgram = CreateSimple2DShader();
    }

    private int CreateSimple2DShader()
    {
        // Simple vertex shader for 2D sprites
        string vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            layout(location = 2) in vec4 aColor;
            
            uniform mat4 uProjection;
            uniform mat4 uView;
            
            out vec2 vTexCoord;
            out vec4 vColor;
            
            void main()
            {
                gl_Position = uProjection * uView * vec4(aPosition, 1.0);
                vTexCoord = aTexCoord;
                vColor = aColor;
            }
        ";
        
        // Fragment shader for textured sprites with color modulation
        string fragmentShaderSource = @"
            #version 330 core
            in vec2 vTexCoord;
            in vec4 vColor;
            out vec4 FragColor;
            
            uniform sampler2D uTexture;
            
            void main()
            {
                vec4 texColor = texture(uTexture, vTexCoord);
                FragColor = texColor * vColor;
            }
        ";
        
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        return program;
    }

    public void BeginBatch()
    {
        if (_batchStarted)
            return;
            
        _batchStarted = true;
        _vertexCount = 0;
        _indexCount = 0;
        _currentColor = Vector4.One;
    }

    public void EndBatch(IShaderManager? shaderManager = null)
    {
        if (!_batchStarted)
            return;
            
        _batchStarted = false;
        
        if (_vertexCount == 0)
            return;
        
        // Upload vertex data to GPU
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.DynamicDraw);
        
        // Upload index data to GPU
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexCount * sizeof(ushort), _indices, BufferUsageHint.DynamicDraw);
        
        // Set up vertex attributes
        GL.EnableVertexAttribArray(0); // aPosition (xyz)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
        
        GL.EnableVertexAttribArray(1); // aTexCoord (uv)
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
        
        GL.EnableVertexAttribArray(2); // aColor (rgba)
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));
        
        // Use our shader program
        GL.UseProgram(_shaderProgram);
        
        // Get uniform locations
        int projLoc = GL.GetUniformLocation(_shaderProgram, "uProjection");
        int viewLoc = GL.GetUniformLocation(_shaderProgram, "uView");
        
        // We'll set these from the calling code
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedShort, 0);
        
        // Reset attribute pointers
        GL.DisableVertexAttribArray(0);
        GL.DisableVertexAttribArray(1);
        GL.DisableVertexAttribArray(2);
    }

    public void DrawQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        if (!_batchStarted)
            BeginBatch();
            
        if (_vertexCount >= MaxVertices - 4)
            EndBatch();
            
        float x = position.X;
        float y = position.Y;
        float w = size.X;
        float h = size.Y;
        
        // Vertex layout: xyz, uv, rgba
        int baseVertex = _vertexCount * 9;
        int baseIndex = _indexCount * 6;
        
        // Quad vertices (counter-clockwise)
        _vertices[baseVertex + 0] = x;       _vertices[baseVertex + 1] = y;       _vertices[baseVertex + 2] = 0.0f; // pos
        _vertices[baseVertex + 3] = 0.0f;    _vertices[baseVertex + 4] = 0.0f;    // uv
        _vertices[baseVertex + 5] = color.X; _vertices[baseVertex + 6] = color.Y; _vertices[baseVertex + 7] = color.Z; _vertices[baseVertex + 8] = color.W; // color
        
        _vertices[baseVertex + 9] = x + w;   _vertices[baseVertex + 10] = y;      _vertices[baseVertex + 11] = 0.0f;
        _vertices[baseVertex + 12] = 1.0f;   _vertices[baseVertex + 13] = 0.0f;
        _vertices[baseVertex + 14] = color.X; _vertices[baseVertex + 15] = color.Y; _vertices[baseVertex + 16] = color.Z; _vertices[baseVertex + 17] = color.W;
        
        _vertices[baseVertex + 18] = x + w;  _vertices[baseVertex + 19] = y + h;  _vertices[baseVertex + 20] = 0.0f;
        _vertices[baseVertex + 21] = 1.0f;   _vertices[baseVertex + 22] = 1.0f;
        _vertices[baseVertex + 23] = color.X; _vertices[baseVertex + 24] = color.Y; _vertices[baseVertex + 25] = color.Z; _vertices[baseVertex + 26] = color.W;
        
        _vertices[baseVertex + 27] = x;      _vertices[baseVertex + 28] = y + h;  _vertices[baseVertex + 29] = 0.0f;
        _vertices[baseVertex + 30] = 0.0f;   _vertices[baseVertex + 31] = 1.0f;
        _vertices[baseVertex + 32] = color.X; _vertices[baseVertex + 33] = color.Y; _vertices[baseVertex + 34] = color.Z; _vertices[baseVertex + 35] = color.W;
        
        // Indices (2 triangles per quad)
        _indices[baseIndex + 0] = (ushort)_vertexCount;
        _indices[baseIndex + 1] = (ushort)(_vertexCount + 1);
        _indices[baseIndex + 2] = (ushort)(_vertexCount + 2);
        _indices[baseIndex + 3] = (ushort)_vertexCount;
        _indices[baseIndex + 4] = (ushort)(_vertexCount + 2);
        _indices[baseIndex + 5] = (ushort)(_vertexCount + 3);
        
        _vertexCount += 4;
        _indexCount += 6;
    }

    public void SetColor(Vector4 color)
    {
        _currentColor = color;
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        if (_shaderProgram > 0)
            GL.DeleteProgram(_shaderProgram);
    }
}
