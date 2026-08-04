using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using Djurspel.Core;

namespace Djurspel.Graphics;

/// <summary>
/// 2D SpriteBatch Renderer som batchar sprite draws för effektiv rendering.
/// Använder vertex buffer för att rendera 2D sprites med färger och transformationer.
/// </summary>
public class SpriteBatchRenderer : ISpriteBatchRenderer, IDisposable
{
    private readonly int _vertexBuffer;
    private readonly int _indexBuffer;
    private readonly int _shaderProgram;
    private readonly int _vao; // VAO för Core Profile
    private Matrix4 _projMatrix;
    private Matrix4 _viewMatrix;
    
    private const int MaxVertices = 65536; // 64K vertices per batch
    private const int MaxIndices = MaxVertices * 3 / 2; // 98K indices (65536 / 4 * 6 per quad)
    
    private readonly float[] _vertices; // xyz, uv, color (9 floats per vertex)
    private readonly uint[] _indices;
    private int _vertexCount;
    private int _indexCount;
    private bool _batchStarted;
    private bool _firstBatchLogged;
    private Vector4 _currentColor = Vector4.One;

    public SpriteBatchRenderer(Matrix4 projMatrix, Matrix4 viewMatrix)
    {
        _projMatrix = projMatrix;
        _viewMatrix = viewMatrix;
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        _vertices = new float[MaxVertices * 9];
        _indices = new uint[MaxIndices];
        _vertexCount = 0;
        _indexCount = 0;
        _batchStarted = false;
        
        // Create a simple 2D shader program
        _shaderProgram = CreateSimple2DShader();
        
        // Create and configure VAO for Core Profile
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));
        GL.BindVertexArray(0); // Unbind
    }

    /// <summary>
    /// Updates projection and view matrices. Call every frame when camera moves.
    /// </summary>
    public void SetMatrices(Matrix4 projMatrix, Matrix4 viewMatrix)
    {
        _projMatrix = projMatrix;
        _viewMatrix = viewMatrix;
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
        
        // Fragment shader for solid-color sprites (no texture needed)
        string fragmentShaderSource = @"
            #version 330 core
            in vec2 vTexCoord;
            in vec4 vColor;
            out vec4 FragColor;
            
            uniform sampler2D uTexture;
            uniform int uHasTexture;
            
            void main()
            {
                if (uHasTexture == 1)
                {
                    vec4 texColor = texture(uTexture, vTexCoord);
                    FragColor = texColor * vColor;
                }
                else
                {
                    FragColor = vColor;
                }
            }
        ";
        
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexOK);
        string? vertexLog = GL.GetShaderInfoLog(vertexShader);
        Console.Error.WriteLine($"[SpriteBatch] Vertex shader compile: {vertexOK}, log: {vertexLog}");
        if (vertexOK == 0) return 0;
        
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragOK);
        string? fragLog = GL.GetShaderInfoLog(fragmentShader);
        Console.Error.WriteLine($"[SpriteBatch] Fragment shader compile: {fragOK}, log: {fragLog}");
        if (fragOK == 0) return 0;
        
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkOK);
        string? linkLog = GL.GetProgramInfoLog(program);
        Console.Error.WriteLine($"[SpriteBatch] Program link: {linkOK}, log: {linkLog}");
        if (linkOK == 0) return 0;
        
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

    public void EndBatch()
    {
        if (_vertexCount == 0) { _batchStarted = false; return; }

        Console.Error.WriteLine($"[SpriteBatch] EndBatch: {(_vertexCount/4)} quads, {_indexCount} indices, proj={_projMatrix.M11:F2}...");
        
        GL.UseProgram(_shaderProgram);
        
        // Bind VAO (vertex attribut state är konfigurerad i konstruktorn)
        GL.BindVertexArray(_vao);
        
        // Upload vertex data to GPU
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexCount * 9 * sizeof(float), _vertices, BufferUsageHint.DynamicDraw);
        Console.Error.WriteLine($"[SpriteBatch] After BufferData VBO: {(int)GL.GetError()}");
        
        // Upload index data to GPU
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexCount * sizeof(uint), _indices, BufferUsageHint.DynamicDraw);
        Console.Error.WriteLine($"[SpriteBatch] After BufferData IBO: {(int)GL.GetError()}");
        
        // Set projection and view matrices DIRECTLY on our shader program
        int projLoc = GL.GetUniformLocation(_shaderProgram, "uProjection");
        int viewLoc = GL.GetUniformLocation(_shaderProgram, "uView");
        
        if (projLoc >= 0 && viewLoc >= 0)
        {
            // transpose=true because C# Matrix4 is row-major, GL expects column-major
            GL.UniformMatrix4(projLoc, true, ref _projMatrix);
            GL.UniformMatrix4(viewLoc, true, ref _viewMatrix);
            
            // DEBUG: print entire projection matrix to stderr
            Console.Error.WriteLine($"[SpriteBatch] proj={_projMatrix.M11:F4},{_projMatrix.M12:F4},{_projMatrix.M13:F4},{_projMatrix.M14:F4} | {_projMatrix.M21:F4},{_projMatrix.M22:F4},{_projMatrix.M23:F4},{_projMatrix.M24:F4} | {_projMatrix.M31:F4},{_projMatrix.M32:F4},{_projMatrix.M33:F4},{_projMatrix.M34:F4} | {_projMatrix.M41:F4},{_projMatrix.M42:F4},{_projMatrix.M43:F4},{_projMatrix.M44:F4}");
            Console.Error.WriteLine($"[SpriteBatch] view={_viewMatrix.M11:F4},{_viewMatrix.M12:F4},{_viewMatrix.M13:F4},{_viewMatrix.M14:F4} | {_viewMatrix.M21:F4},{_viewMatrix.M22:F4},{_viewMatrix.M23:F4},{_viewMatrix.M24:F4} | {_viewMatrix.M31:F4},{_viewMatrix.M32:F4},{_viewMatrix.M33:F4},{_viewMatrix.M34:F4} | {_viewMatrix.M41:F4},{_viewMatrix.M42:F4},{_viewMatrix.M43:F4},{_viewMatrix.M44:F4}");
            // First batch: log first quad's vertices
            if (!_firstBatchLogged)
            {
                _firstBatchLogged = true;
                Console.Error.WriteLine($"[SpriteBatch] DEBUG: first quad verts (xyz,uv,rgba):");
                Console.Error.WriteLine($"  v0: {_vertices[0]:F2},{_vertices[1]:F2},{_vertices[2]:F2} uv:{_vertices[3]:F2},{_vertices[4]:F2} color:{_vertices[5]:F2},{_vertices[6]:F2},{_vertices[7]:F2},{_vertices[8]:F2}");
                Console.Error.WriteLine($"  v1: {_vertices[9]:F2},{_vertices[10]:F2},{_vertices[11]:F2} uv:{_vertices[12]:F2},{_vertices[13]:F2} color:{_vertices[14]:F2},{_vertices[15]:F2},{_vertices[16]:F2},{_vertices[17]:F2}");
                Console.Error.WriteLine($"  v2: {_vertices[18]:F2},{_vertices[19]:F2},{_vertices[20]:F2} uv:{_vertices[21]:F2},{_vertices[22]:F2} color:{_vertices[23]:F2},{_vertices[24]:F2},{_vertices[25]:F2},{_vertices[26]:F2}");
                Console.Error.WriteLine($"  v3: {_vertices[27]:F2},{_vertices[28]:F2},{_vertices[29]:F2} uv:{_vertices[30]:F2},{_vertices[31]:F2} color:{_vertices[32]:F2},{_vertices[33]:F2},{_vertices[34]:F2},{_vertices[35]:F2}");
            }
        }
        else
        {
            Console.Error.WriteLine($"[SpriteBatch] WARNING: uniform locations not found! proj={projLoc}, view={viewLoc}");
        }
        
        // Set texture flag (no texture bound = solid color)
        int hasTexLoc = GL.GetUniformLocation(_shaderProgram, "uHasTexture");
        if (hasTexLoc >= 0)
        {
            GL.Uniform1(hasTexLoc, 0); // 0 = no texture, use vColor directly
        }
        
        // Pre-draw error check
        int preErr = (int)GL.GetError();
        if (preErr != 0)
            Console.Error.WriteLine($"[SpriteBatch] GL Error BEFORE DrawElements: {preErr}");
        
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
        
               // Post-draw error check
               int postDrawErr = (int)GL.GetError();
               if (postDrawErr != 0)
                   Console.Error.WriteLine($"[SpriteBatch] POST-DRAW-ERROR: {postDrawErr} after DrawElements({postDrawErr})");
        
        // Check for GL errors after draw
        int err = (int)GL.GetError();
        if (err != 0)
            Console.Error.WriteLine($"[SpriteBatch] GL Error after DrawElements: {err}");
        
        // Unbind VAO
        GL.BindVertexArray(0);
        
        // Reset batch state for next frame
        _vertexCount = 0;
        _indexCount = 0;
        _batchStarted = false;
    }

    public void DrawQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        if (!_batchStarted)
            BeginBatch();
            
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
        _indices[baseIndex + 0] = (uint)_vertexCount;
        _indices[baseIndex + 1] = (uint)(_vertexCount + 1);
        _indices[baseIndex + 2] = (uint)(_vertexCount + 2);
        _indices[baseIndex + 3] = (uint)_vertexCount;
        _indices[baseIndex + 4] = (uint)(_vertexCount + 2);
        _indices[baseIndex + 5] = (uint)(_vertexCount + 3);
        
        _vertexCount += 4;
        _indexCount += 6;
    }

    public void SetColor(Vector4 color)
    {
        _currentColor = color;
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        if (_shaderProgram > 0)
            GL.DeleteProgram(_shaderProgram);
    }
}
