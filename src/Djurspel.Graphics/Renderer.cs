using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using Djurspel.Core;
using Djurspel.Entities;
using Djurspel.Entities.Components;

namespace Djurspel.Graphics;

/// <summary>
/// OpenGL-based renderer for the Djurspel engine.
/// Uses dynamic dispatch for game-world types to avoid circular dependencies.
/// </summary>
public class Renderer : IRenderer
{
    private const float TILE_SIZE = 1.0f;
    private readonly int _windowWidth;
    private readonly int _windowHeight;
    private IShaderManager? _shaderManager;
    private int _sceneShaderProgram = 0;
    private int? _spriteShaderProgram = null;
    private ITextureLoader? _textureLoader;
    private readonly Dictionary<string, TextureAsset> _textures = new();

    /// <summary>Cache for tile-mesh VAOs keyed by TileType name.</summary>
    private readonly Dictionary<string, (int vao, int vbo, int elementCount)> _tileMeshes = new();
    private int _spriteVao = 0, _spriteVbo = 0, _spriteEbo = 0, _spriteIndexCount = 0;

    public Renderer(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    /// <summary>Set the texture loader for sprite loading.</summary>
    public void SetTextureLoader(ITextureLoader loader)
    {
        _textureLoader = loader;
    }

    public void Dispose()
     {
         CleanupShaderProgram();
         if (_spriteShaderProgram != null) GL.DeleteProgram(_spriteShaderProgram.Value);
         if (_spriteVao != 0) GL.DeleteVertexArray(_spriteVao);
         if (_spriteVbo != 0) GL.DeleteBuffer(_spriteVbo);
         if (_spriteEbo != 0) GL.DeleteBuffer(_spriteEbo);
         foreach (var tex in _textures.Values)
         {
             GL.DeleteTexture(tex.GlHandle);
         }
         foreach (var (_, (vao, _, _)) in _tileMeshes)
         {
             GL.DeleteVertexArray(vao);
         }
     }

     public void SetShaderManager(IShaderManager shaderManager)
     {
         _shaderManager = shaderManager;
     }

     public void Initialize()
    {
        GL.Viewport(0, 0, _windowWidth, _windowHeight);
        Console.Error.WriteLine($"[Initialize] Viewport set to 0,0,{_windowWidth},{_windowHeight}");
        GL.ClearColor(0.15f, 0.15f, 0.2f, 1.0f);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        Console.Error.WriteLine($"[Initialize] DepthTest disabled, CullFace disabled");
        BuildSpriteMesh();
    }

    #region Sprite Mesh

    /// <summary>Create a quad mesh (2 triangles, 4 verts) for textured sprites.</summary>
    private void BuildSpriteMesh()
    {
        // Quad vertices: pos(3) + uv(2) = 5 floats per vertex
        // UV coords: (0,0) top-left, (1,1) bottom-right
        float[] verts =
        {
            // pos.x   pos.y   pos.z   u   v
            -0.5f, -0.5f, 0f, 0f, 1f,
             0.5f, -0.5f, 0f, 1f, 1f,
             0.5f,  0.5f, 0f, 1f, 0f,
            -0.5f,  0.5f, 0f, 0f, 0f,
        };
        uint[] indices = { 0, 1, 2, 0, 2, 3 };

        _spriteVao = GL.GenVertexArray();
        GL.BindVertexArray(_spriteVao);

        _spriteVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _spriteVbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

        _spriteEbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _spriteEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // Position attribute (location 0)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        // UV attribute (location 1)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);
        _spriteIndexCount = indices.Length;
    }

    #endregion

    #region Render

    public void Render(ICamera camera, IShaderManager shaderManager, float frameTime)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shaderManager = shaderManager;
        int sceneProg = EnsureSceneShader(shaderManager);
        if (sceneProg == 0) return;

        // Set view matrix from camera position
        Vector3 camPos = camera.Position;
        Matrix4 view = Matrix4.CreateTranslation(-camPos.X, -camPos.Y, 0);

        // Set projection
        float aspect = (float)_windowWidth / (float)_windowHeight;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(
            (float)Math.PI / 4.0f, aspect, 0.1f, 1000.0f);

        SetUniformMat4(sceneProg, "uView", ref view);
        SetUniformMat4(sceneProg, "uProjection", ref proj);

        // Draw tile map
        DrawTileMap(camera, 0.0f);

        // Draw entities with sprites
        DrawEntity(camera, 0.0f);
    }

    #endregion

    #region Tile Map Rendering (public IRenderer interface)

    /// <summary>
    /// Draws the tile map from the given world/region objects using dynamic dispatch.
    /// </summary>
    public void DrawTileMap(object world, object region, float interpolation)
    {
        // Use the passed world/region directly instead of _dummyWorld
        dynamic w = world;
        int width = w.Width;
        int height = w.Height;

        // Draw only the tiles in this specific region
        dynamic rd = region;
        int startX = rd.Origin.X;
        int startY = rd.Origin.Y;
        int sizeX = rd.Size.X;
        int sizeY = rd.Size.Y;
        int layer = rd.Layer;

        if (_shaderManager == null) return;
        int prog = EnsureSceneShader(_shaderManager);
        if (prog == 0) return;

        for (int y = startY; y < startY + sizeY && y < height; y++)
        {
            for (int x = startX; x < startX + sizeX && x < width; x++)
            {
                dynamic tile = w.GetTile(x, y, layer);
                string tileType = tile.Type.ToString();
                if (tileType == "Void") continue;

                string key = tileType;
                if (!_tileMeshes.ContainsKey(key))
                {
                    Vector3 size = GetTileScale(tileType);
                    var mesh = CreateBoxMesh(size);
                    _tileMeshes[key] = mesh;
                }

                var (vao, _, elemCount) = _tileMeshes[key];

                Vector3 pos = new(x * TILE_SIZE, layer * TILE_SIZE, y * TILE_SIZE);
                Matrix4 model = Matrix4.CreateScale(GetTileScale(tileType))
                    * Matrix4.CreateTranslation(pos);
                SetUniformMat4(prog, "uModel", ref model);

                Vector4 col;
                try
                {
                    if (tile.HasProperty("TintColor") && tile.TintColor != null)
                    {
                        var tint = tile.TintColor;
                        col = new Vector4(tint.X, tint.Y, tint.Z, tint.W);
                    }
                    else
                    {
                        col = TileColor(tileType);
                    }
                }
                catch
                {
                    col = TileColor(tileType);
                }
                SetUniform4(prog, "uColor", col);

                GL.BindVertexArray(vao);
                GL.DrawElements(PrimitiveType.Triangles, elemCount, DrawElementsType.UnsignedInt, 0);
                GL.BindVertexArray(0);
            }
        }
    }

    private void DrawTileMap(ICamera camera, float interpolation)
    {
        if (_shaderManager == null) return;
        int prog = EnsureSceneShader(_shaderManager);
        if (prog == 0) return;

        dynamic w = _dummyWorld;
        int width = w.Width;
        int height = w.Height;

        var regions = w.GetVisibleTiles();
        foreach (dynamic rd in regions)
        {
            int startX = rd.Origin.X;
            int startY = rd.Origin.Y;
            int sizeX = rd.Size.X;
            int sizeY = rd.Size.Y;
            int layer = rd.Layer;

            for (int y = startY; y < startY + sizeY && y < height; y++)
            {
                for (int x = startX; x < startX + sizeX && x < width; x++)
                {
                    dynamic tile = w.GetTile(x, y, layer);
                    string tileType = tile.Type.ToString();
                    if (tileType == "Void") continue;

                    string key = tileType;
                    if (!_tileMeshes.ContainsKey(key))
                    {
                        Vector3 size = GetTileScale(tileType);
                        var mesh = CreateBoxMesh(size);
                        _tileMeshes[key] = mesh;
                    }

                    var (vao, _, elemCount) = _tileMeshes[key];

                    Vector3 pos = new(x * TILE_SIZE, layer * TILE_SIZE, y * TILE_SIZE);
                    Matrix4 model = Matrix4.CreateScale(GetTileScale(tileType))
                        * Matrix4.CreateTranslation(pos);
                    SetUniformMat4(prog, "uModel", ref model);

                    Vector4 col;
                    try
                    {
                        if (tile.HasProperty("TintColor") && tile.TintColor != null)
                        {
                            var tint = tile.TintColor;
                            col = new Vector4(tint.X, tint.Y, tint.Z, tint.W);
                        }
                        else
                        {
                            col = TileColor(tileType);
                        }
                    }
                    catch
                    {
                        col = TileColor(tileType);
                    }
                    SetUniform4(prog, "uColor", col);

                    GL.BindVertexArray(vao);
                    GL.DrawElements(PrimitiveType.Triangles, elemCount, DrawElementsType.UnsignedInt, 0);
                    GL.BindVertexArray(0);
                }
            }
        }
    }

    #endregion

    #region Entity Rendering (public IRenderer interface)

    /// <summary>
    /// Draws a single entity. Uses dynamic dispatch to get position and render component.
    /// If SpriteName is set, draws a textured billboard quad.
    /// </summary>
    public void DrawEntity(object entity, float interpolation)
    {
        dynamic e = entity;

        // Get position from TransformComponent
        try
        {
            var transform = e.GetComponent<TransformComponent>();
            if (transform == null) return;
            float tx = transform.X, ty = transform.Y, tz = transform.Z;
            if (float.IsNaN(tx)) tx = 0f;
            if (float.IsNaN(ty)) ty = 0f;
            if (float.IsNaN(tz)) tz = 0f;
            Vector3 worldPos = new(tx, ty, tz);

            // Try to get RenderComponent
            string? spriteName = null;
            try
            {
                var renderComp = e.GetComponent<RenderComponent>();
                if (renderComp is null || !renderComp.Visible) return;
                spriteName = renderComp.SpriteName;
            }
            catch
            {
                return;
            }

            if (string.IsNullOrEmpty(spriteName)) return;

            // Ensure sprite shader
            if (_spriteShaderProgram is null || _spriteShaderProgram == 0)
            {
                int spriteProg = BuildSpriteShader();
                if (spriteProg == 0) return;
                _spriteShaderProgram = spriteProg;
            }
            int prog = _spriteShaderProgram!.Value;

            // Load or get cached texture
            string assetPath = $"sprites/{spriteName}";
            if (!_textures.ContainsKey(assetPath) && _textureLoader != null)
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", $"{spriteName}.bmp");
                if (!File.Exists(fullPath))
                {
                    fullPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", $"{spriteName}.bmp");
                }
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var asset = _textureLoader.LoadTexture(fullPath);
                        _textures[assetPath] = asset;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to load texture {fullPath}: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Texture not found: {fullPath}, using colored cube fallback");
                    DrawEntityAsCube(e, worldPos);
                    return;
                }
            }

            if (!_textures.TryGetValue(assetPath, out var tex))
            {
                DrawEntityAsCube(e, worldPos);
                return;
            }

            float spriteScale = 1.5f;
            Vector3 center = new(worldPos.X, worldPos.Y, worldPos.Z);
            Matrix4 model = Matrix4.CreateTranslation(center.X, center.Y, center.Z)
                * Matrix4.CreateScale(spriteScale, spriteScale, spriteScale);

            SetUniformMat4(prog, "uModel", ref model);
            SetUniform1(prog, "uTexture", 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex.GlHandle);

            GL.BindVertexArray(_spriteVao);
            GL.DrawElements(PrimitiveType.Triangles, _spriteIndexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DrawEntity] Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void DrawEntityAsCube(dynamic e, Vector3 worldPos)
    {
        if (_shaderManager == null) return;
        int prog = EnsureSceneShader(_shaderManager);
        if (prog == 0) return;

        Vector3 size = new(0.5f, 0.5f, 0.5f);
        Matrix4 model = Matrix4.CreateTranslation(worldPos)
            * Matrix4.CreateScale(size);
        SetUniformMat4(prog, "uModel", ref model);

        Vector4 col = new(0.3f, 0.6f, 1.0f, 1.0f);
        SetUniform4(prog, "uColor", col);

        if (!_entityMeshes.ContainsKey("_cube_"))
        {
            _entityMeshes["_cube_"] = CreateBoxMesh(size);
        }
        var (vao, _, elemCount) = _entityMeshes["_cube_"];
        GL.UseProgram(prog);
        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, elemCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        Console.Error.WriteLine($"[DrawEntityAsCube] Drew cube at {worldPos}, err={(int)GL.GetError()}");
    }

    // --- stubs for scene access ---
    private readonly object _dummyWorld = null!;
    private readonly object _dummyEntity = null!;

    #endregion

    #region Grid Rendering

    private void DrawGrid(int shaderProg, Matrix4 view)
    {
        SetUniform4(shaderProg, "uColor", new Vector4(0.3f, 0.3f, 0.3f, 0.5f));

        const int gridSize = 20;
        const float step = 1.0f;

        List<float> gridVerts = new();
        for (int i = 0; i <= gridSize; i++)
        {
            float pos = i * step - gridSize / 2f;
            gridVerts.Add(pos); gridVerts.Add(0f); gridVerts.Add(-gridSize / 2f);
            gridVerts.Add(pos); gridVerts.Add(0f); gridVerts.Add(gridSize / 2f);
            gridVerts.Add(-gridSize / 2f); gridVerts.Add(0f); gridVerts.Add(pos);
            gridVerts.Add(gridSize / 2f); gridVerts.Add(0f); gridVerts.Add(pos);
        }

        Matrix4 model = Matrix4.Identity;
        SetUniformMat4(shaderProg, "uModel", ref model);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            gridVerts.Count * sizeof(float), gridVerts.ToArray(), BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);

        GL.Enable(EnableCap.LineSmooth);
        GL.LineWidth(1.0f);
        GL.DrawArrays(PrimitiveType.Lines, 0, gridVerts.Count / 3);
        GL.Disable(EnableCap.LineSmooth);
        GL.DeleteBuffer(vbo);
    }

    #endregion

    #region Shader Management

    private int EnsureSceneShader(IShaderManager shaderManager)
    {
        if (_sceneShaderProgram != 0) return _sceneShaderProgram;

        try
        {
            var prog = shaderManager.Load("scene",
                @"#version 330 core
                layout(location = 0) in vec3 aPos;
                layout(location = 1) in vec3 aNormal;
                out vec3 vNormal;
                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProjection;
                void main()
                {
                    gl_Position = uProjection * uView * uModel * vec4(aPos, 1.0);
                    vNormal = mat3(uModel) * aNormal;
                }",
                @"#version 330 core
                in vec3 vNormal;
                out vec4 FragColor;
                uniform vec4 uColor;
                void main()
                {
                    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
                    float diff = max(dot(normalize(vNormal), lightDir), 0.0);
                    vec3 ambient = vec3(0.3) * uColor.rgb;
                    vec3 diffuse = diff * uColor.rgb;
                    FragColor = vec4(ambient + diffuse, uColor.a);
                }");
            _sceneShaderProgram = prog.GlProgramId;
            GL.UseProgram(_sceneShaderProgram);
            Console.Error.WriteLine($"[EnsureSceneShader] Loaded scene shader, prog={_sceneShaderProgram}");
            int status;
            GL.GetProgram(_sceneShaderProgram, GetProgramParameterName.LinkStatus, out status);
            Console.Error.WriteLine($"[EnsureSceneShader] Link status: {status}");
            if (status == 0) {
                string infoLog = GL.GetProgramInfoLog(_sceneShaderProgram);
                Console.Error.WriteLine($"[EnsureSceneShader] Link error: {infoLog}");
                GL.DeleteProgram(_sceneShaderProgram);
                _sceneShaderProgram = 0;
                return 0;
            }
            Console.Error.WriteLine("[EnsureSceneShader] Shader linked OK");
            return _sceneShaderProgram;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Shader load failed: {ex.Message}");
            return 0;
        }
    }

    /// <summary>Build the sprite shader program (texture + model matrix).</summary>
    private int BuildSpriteShader()
    {
        var shaderManager = _shaderManager ?? throw new InvalidOperationException("No shader manager available");
        try
        {
            var prog = shaderManager.Load("sprite",
                @"#version 330 core
                layout(location = 0) in vec3 aPos;
                layout(location = 1) in vec2 aUV;
                out vec2 vUV;
                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProjection;
                void main()
                {
                    vUV = aUV;
                    gl_Position = uProjection * uView * uModel * vec4(aPos, 1.0);
                }",
                @"#version 330 core
                in vec2 vUV;
                out vec4 FragColor;
                uniform sampler2D uTexture;
                void main()
                {
                    FragColor = texture(uTexture, vUV);
                }");
            return prog.GlProgramId;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Sprite shader load failed: {ex.Message}");
            return 0;
        }
    }

    private void CleanupShaderProgram()
    {
        if (_sceneShaderProgram != 0)
        {
            GL.DeleteProgram(_sceneShaderProgram);
            _sceneShaderProgram = 0;
        }
    }

    private void SetUniformMat4(int prog, string name, ref Matrix4 mat)
    {
        int loc = GL.GetUniformLocation(prog, name);
        if (loc >= 0) GL.UniformMatrix4(loc, false, ref mat);
    }

    private void SetUniform4(int prog, string name, Vector4 color)
    {
        int loc = GL.GetUniformLocation(prog, name);
        if (loc >= 0) GL.Uniform4(loc, color.X, color.Y, color.Z, color.W);
    }

    private void SetUniform1(int prog, string name, int value)
    {
        int loc = GL.GetUniformLocation(prog, name);
        if (loc >= 0) GL.Uniform1(loc, value);
    }

    #endregion

    #region Primitives

    public void DrawCube(Vector3 position, Vector3 size, Vector4 color, IShaderManager shaderManager)
    {
        int prog = EnsureSceneShader(shaderManager);
        if (prog == 0) return;
        Matrix4 model = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(position);
        SetUniformMat4(prog, "uModel", ref model);
        SetUniform4(prog, "uColor", color);

        if (!_entityMeshes.ContainsKey("_cube_"))
        {
            _entityMeshes["_cube_"] = CreateBoxMesh(size);
        }
        var (vao, _, elemCount) = _entityMeshes["_cube_"];
        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, elemCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void DrawSphere(Vector3 position, float radius, Vector4 color, IShaderManager shaderManager)
    {
        Vector3 size = new(radius * 2f, radius * 2f, radius * 2f);
        DrawCube(position, size, color, shaderManager);
    }

    public void DrawCylinder(Vector3 position, float radius, float height, Vector4 color, IShaderManager shaderManager)
    {
        DrawCube(position, new(radius * 2f, height, radius * 2f), color, shaderManager);
    }

    public void DrawPlane(Vector3 position, Vector2 size, Vector4 color, IShaderManager shaderManager)
    {
        DrawCube(position, new(size.X, 0.01f, size.Y), color, shaderManager);
    }

    public void BeginScene()
    {
        Console.Error.WriteLine($"[BeginScene] Win {_windowWidth}x{_windowHeight}");
        GL.Clear(ClearBufferMask.ColorBufferBit);
        ErrorCode err1 = GL.GetError();
        Console.Error.WriteLine($"[BeginScene] Clear error: {err1}");
    }

    public void EndScene()
    {
        // Swap buffers — stubbed in headless context
    }

    #endregion

    #region Geometry Helpers

    private static readonly Dictionary<string, (int vao, int vbo, int elementCount)> _entityMeshes = new();

    private static (int vao, int vbo, int elementCount) CreateBoxMesh(Vector3 size)
    {
        float hx = size.X / 2f, hy = size.Y / 2f, hz = size.Z / 2f;

        float[] vertices = new float[48]; // 8 verts * 6 floats

        // Front face
        vertices[0] = -hx; vertices[1] = -hy; vertices[2] = hz;
        vertices[3] = 0; vertices[4] = 0; vertices[5] = 1;
        vertices[6] = hx; vertices[7] = -hy; vertices[8] = hz;
        vertices[9] = 0; vertices[10] = 0; vertices[11] = 1;
        vertices[12] = hx; vertices[13] = hy; vertices[14] = hz;
        vertices[15] = 0; vertices[16] = 0; vertices[17] = 1;
        vertices[18] = -hx; vertices[19] = hy; vertices[20] = hz;
        vertices[21] = 0; vertices[22] = 0; vertices[23] = 1;

        // Back face
        vertices[24] = hx; vertices[25] = -hy; vertices[26] = -hz;
        vertices[27] = 0; vertices[28] = 0; vertices[29] = -1;
        vertices[30] = -hx; vertices[31] = -hy; vertices[32] = -hz;
        vertices[33] = 0; vertices[34] = 0; vertices[35] = -1;
        vertices[36] = -hx; vertices[37] = hy; vertices[38] = -hz;
        vertices[39] = 0; vertices[40] = 0; vertices[41] = -1;
        vertices[42] = hx; vertices[43] = hy; vertices[44] = -hz;
        vertices[45] = 0; vertices[46] = 0; vertices[47] = -1;

        uint[] indices = new uint[36];
        indices[0] = 0; indices[1] = 1; indices[2] = 2;  indices[3] = 0; indices[4] = 2; indices[5] = 3;
        indices[6] = 4; indices[7] = 5; indices[8] = 6;  indices[9] = 4; indices[10] = 6; indices[11] = 7;
        indices[12] = 3; indices[13] = 2; indices[14] = 6;  indices[15] = 3; indices[16] = 6; indices[17] = 7;
        indices[18] = 0; indices[19] = 3; indices[20] = 5;  indices[21] = 0; indices[22] = 5; indices[23] = 4;
        indices[24] = 1; indices[25] = 5; indices[26] = 6;  indices[27] = 1; indices[28] = 6; indices[29] = 2;
        indices[30] = 0; indices[31] = 4; indices[32] = 7;  indices[33] = 0; indices[34] = 7; indices[35] = 3;

        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 6, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof(float) * 6, sizeof(float) * 3);

        int ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);

        return (vao, vbo, indices.Length);
    }

    private Vector3 GetTileScale(string tileType)
    {
        return tileType switch
        {
            "Wall" => new Vector3(TILE_SIZE, TILE_SIZE * 2f, TILE_SIZE),
            "Water" => new Vector3(TILE_SIZE, TILE_SIZE * 0.3f, TILE_SIZE),
            "Floor" => new Vector3(TILE_SIZE, TILE_SIZE * 0.1f, TILE_SIZE),
            "Door" => new Vector3(TILE_SIZE, TILE_SIZE * 1.5f, TILE_SIZE),
            "Stairs" => new Vector3(TILE_SIZE, TILE_SIZE * 0.5f, TILE_SIZE),
            "Ground" => new Vector3(TILE_SIZE, TILE_SIZE, TILE_SIZE),
            _ => new Vector3(TILE_SIZE, TILE_SIZE, TILE_SIZE),
        };
    }

    private Vector4 TileColor(string tileType)
    {
        return tileType switch
        {
            "Wall" => new Vector4(0.4f, 0.35f, 0.3f, 1.0f),
            "Water" => new Vector4(0.2f, 0.4f, 0.8f, 0.7f),
            "Ground" => new Vector4(0.3f, 0.6f, 0.3f, 1.0f),
            "Floor" => new Vector4(0.6f, 0.55f, 0.5f, 1.0f),
            "Door" => new Vector4(0.5f, 0.4f, 0.2f, 1.0f),
            "Stairs" => new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
        };
    }

    #endregion
}
