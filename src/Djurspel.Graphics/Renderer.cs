using System;
using System.Collections.Generic;
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

    /// <summary>Cache for tile-mesh VAOs keyed by TileType name.</summary>
    private readonly Dictionary<string, (int vao, int vbo, int elementCount)> _tileMeshes = new();
    private readonly Dictionary<string, (int vao, int vbo, int elementCount)> _entityMeshes = new();

    public Renderer(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void Dispose()
    {
        CleanupShaderProgram();
        foreach (var (_, (vao, _, _)) in _tileMeshes)
        {
            GL.DeleteVertexArray(vao);
        }
        foreach (var (_, (vao, _, _)) in _entityMeshes)
        {
            GL.DeleteVertexArray(vao);
        }
    }

    public void Initialize()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.ClearColor(0.15f, 0.15f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.Viewport(0, 0, _windowWidth, _windowHeight);
    }

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

        // Draw entities
        DrawEntity(camera, 0.0f);
    }

    #region Tile Map Rendering (public IRenderer interface)

    /// <summary>
    /// Draws the tile map from the given world/region objects using dynamic dispatch.
    /// </summary>
    public void DrawTileMap(object world, object region, float interpolation)
    {
        // Delegate to the camera-based overload
        dynamic cam = world;
        DrawTileMap(cam, interpolation);
    }

    private void DrawTileMap(ICamera camera, float interpolation)
    {
        if (_shaderManager == null) return;
        int prog = EnsureSceneShader(_shaderManager);
        if (prog == 0) return;

        // Get world via dynamic dispatch
        dynamic w = _dummyWorld;
        int width = w.Width;
        int height = w.Height;

        // Get tile draw regions
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

                    // Get or create tile mesh
                    string key = tileType;
                    if (!_tileMeshes.ContainsKey(key))
                    {
                        Vector3 size = GetTileScale(tileType);
                        var mesh = CreateBoxMesh(size);
                        _tileMeshes[key] = mesh;
                    }

                    var (vao, _, elemCount) = _tileMeshes[key];

                    // Model matrix: translate to tile position, scale
                    Vector3 pos = new(x * TILE_SIZE, layer * TILE_SIZE, y * TILE_SIZE);
                    Matrix4 model = Matrix4.CreateScale(GetTileScale(tileType))
                        * Matrix4.CreateTranslation(pos);
                    SetUniformMat4(prog, "uModel", ref model);

                    // Color - use tile's TintColor if available, otherwise default
                    Vector4 col;
                    try
                    {
                        if (tile.HasProperty("TintColor") && tile.TintColor != null)
                        {
                            // System.Numerics.Vector4 -> OpenTK.Vector4
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

                    // Draw
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
    /// </summary>
    public void DrawEntity(object entity, float interpolation)
    {
        // Delegate to the camera-based overload
        dynamic cam = entity;
        DrawEntity(cam, interpolation);
    }

    private void DrawEntity(ICamera camera, float interpolation)
    {
        if (_shaderManager == null) return;
        int prog = EnsureSceneShader(_shaderManager);
        if (prog == 0) return;

        // Get world via dynamic dispatch
        dynamic e = _dummyEntity;

        // Get position via dynamic dispatch (Vec3I or Vector3)
        dynamic pos = e.Position;
        Vector3 worldPos = new(pos.X, pos.Y, pos.Z);

        // Try to get RenderComponent
        try
        {
            var renderComp = e.GetComponent<RenderComponent>();
            if (renderComp == null || !renderComp.Visible) return;
        }
        catch
        {
            // Entity might not have components yet; draw anyway
        }

        // Use default entity mesh (small cube)
        string key = "entity_default";
        if (!_entityMeshes.ContainsKey(key))
        {
            var mesh = CreateBoxMesh(new Vector3(0.5f, 0.5f, 0.5f));
            _entityMeshes[key] = mesh;
        }

        var (vao, _, elemCount) = _entityMeshes[key];
        Matrix4 model = Matrix4.CreateTranslation(worldPos);
        SetUniformMat4(prog, "uModel", ref model);

        Vector4 col = new(1.0f, 0.5f, 0.0f, 1.0f);
        SetUniform4(prog, "uColor", col);

        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, elemCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

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
            return _sceneShaderProgram;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Shader load failed: {ex.Message}");
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
        // Stub: draw a small cube as placeholder for sphere
        Vector3 size = new(radius * 2f, radius * 2f, radius * 2f);
        DrawCube(position, size, color, shaderManager);
    }

    public void DrawCylinder(Vector3 position, float radius, float height, Vector4 color, IShaderManager shaderManager)
    {
        // Stub: draw a scaled cube as placeholder
        DrawCube(position, new(radius * 2f, height, radius * 2f), color, shaderManager);
    }

    public void DrawPlane(Vector3 position, Vector2 size, Vector4 color, IShaderManager shaderManager)
    {
        // Stub: draw a flat cube
        DrawCube(position, new(size.X, 0.01f, size.Y), color, shaderManager);
    }

    public void BeginScene()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
    }

    public void EndScene()
    {
        // Swap buffers — stubbed in headless context
    }

    #endregion

    #region Geometry Helpers

    private static (int vao, int vbo, int elementCount) CreateBoxMesh(Vector3 size)
    {
        float hx = size.X / 2f, hy = size.Y / 2f, hz = size.Z / 2f;

        // 8 vertices: position (3) + normal (3) = 6 floats per vertex
        float[] vertices = new float[48]; // 8 verts * 6 floats

        // Front face (z = +hz)
        vertices[0] = -hx; vertices[1] = -hy; vertices[2] = hz;
        vertices[3] = 0; vertices[4] = 0; vertices[5] = 1;
        vertices[6] = hx; vertices[7] = -hy; vertices[8] = hz;
        vertices[9] = 0; vertices[10] = 0; vertices[11] = 1;
        vertices[12] = hx; vertices[13] = hy; vertices[14] = hz;
        vertices[15] = 0; vertices[16] = 0; vertices[17] = 1;
        vertices[18] = -hx; vertices[19] = hy; vertices[20] = hz;
        vertices[21] = 0; vertices[22] = 0; vertices[23] = 1;

        // Back face (z = -hz)
        vertices[24] = hx; vertices[25] = -hy; vertices[26] = -hz;
        vertices[27] = 0; vertices[28] = 0; vertices[29] = -1;
        vertices[30] = -hx; vertices[31] = -hy; vertices[32] = -hz;
        vertices[33] = 0; vertices[34] = 0; vertices[35] = -1;
        vertices[36] = -hx; vertices[37] = hy; vertices[38] = -hz;
        vertices[39] = 0; vertices[40] = 0; vertices[41] = -1;
        vertices[42] = hx; vertices[43] = hy; vertices[44] = -hz;
        vertices[45] = 0; vertices[46] = 0; vertices[47] = -1;

        // 36 indices
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

    // --- stubs for scene access (wired when IWorld/IRegion exist) ---
    private readonly object _dummyWorld = null!;
    private readonly object _dummyRegion = null!;
    private readonly object _dummyEntity = null!;
}
