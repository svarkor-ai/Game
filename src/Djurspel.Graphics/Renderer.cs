using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System;

namespace Djurspel.Graphics;

/// <summary>
/// OpenGL renderer — VBO-based rendering for primitives.
/// Uses Core Profile (VAO/VBO) for OpenTK 4 compatibility.
/// </summary>
public class Renderer : IRenderer
{
    private readonly int _cubeVAO;
    private readonly int _cubeVBO;
    private readonly int _cubeEBO;

    // Unit cube vertices (8 corners x 3 floats)
    private readonly float[] _cubeVertices = {
        -0.5f, -0.5f, -0.5f,   0.5f, -0.5f, -0.5f,   0.5f,  0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,
        -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f,  0.5f,  0.5f,  -0.5f,  0.5f,  0.5f
    };

    // Cube indices (6 faces x 2 triangles x 3 indices)
    private readonly int[] _cubeIndices = {
        0, 1, 2, 2, 3, 0,   // Front
        4, 5, 6, 6, 7, 4,   // Back
        4, 0, 3, 3, 7, 4,   // Left
        1, 5, 6, 6, 2, 1,   // Right
        3, 2, 6, 6, 7, 3,   // Top
        4, 5, 1, 1, 0, 4    // Bottom
    };

    public Renderer()
    {
        // Create cube VAO/VBO/EBO
        _cubeVAO = GL.GenVertexArray();
        _cubeVBO = GL.GenBuffer();
        _cubeEBO = GL.GenBuffer();

        GL.BindVertexArray(_cubeVAO);

        // Upload vertex data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, _cubeVertices.Length * sizeof(float), _cubeVertices, BufferUsageHint.StaticDraw);

        // Upload index data
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _cubeIndices.Length * sizeof(int), _cubeIndices, BufferUsageHint.StaticDraw);

        // Vertex attribute pointer
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        GL.BindVertexArray(0);
    }

    public void Initialize()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.ClearColor(0.15f, 0.15f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
    }

    public void Render(ICamera camera, IShaderManager shaderManager, float frameTime)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shaderManager.SetMat4("uViewMatrix", BuildViewMatrix(camera));
        shaderManager.SetMat4("uProjectionMatrix", BuildProjectionMatrix());
        shaderManager.SetFloat("uTime", frameTime);

        // Draw grid
        DrawGrid(shaderManager);
    }

    public void DrawCube(Vector3 position, Vector3 size, Vector4 color, IShaderManager shaderManager)
    {
        GL.BindVertexArray(_cubeVAO);

        shaderManager.SetVec3("uModelPosition", position);
        shaderManager.SetVec3("uModelSize", size);
        shaderManager.SetVec4("uColor", color);

        GL.DrawElements(PrimitiveType.Triangles, _cubeIndices.Length, DrawElementsType.UnsignedInt, _cubeIndices);

        GL.BindVertexArray(0);
    }

    public void DrawSphere(Vector3 position, float radius, Vector4 color, IShaderManager shaderManager)
    {
        // Stub — sphere mesh to be generated later
        DrawCube(position, new Vector3(radius * 2, radius * 2, radius * 2), color, shaderManager);
    }

    public void DrawCylinder(Vector3 position, float radius, float height, Vector4 color, IShaderManager shaderManager)
    {
        // Stub — cylinder mesh to be generated later
        DrawCube(position, new Vector3(radius * 2, height, radius * 2), color, shaderManager);
    }

    public void DrawPlane(Vector3 position, Vector2 size, Vector4 color, IShaderManager shaderManager)
    {
        // Stub — plane mesh to be generated later
        DrawCube(position, new Vector3(size.X, 0.01f, size.Y), color, shaderManager);
    }

    // ---- Game scene methods ----

    public void BeginScene()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
    }

    public void EndScene()
    {
        // Present — stubbed (swap buffers in real GL context)
    }

    public void DrawTileMap(object world, object region, float interpolation)
    {
        // Stub — tile map rendering would use batched VBOs with the world shader
    }

    public void DrawEntity(object entity, float interpolation)
    {
        // Stub — entity rendering would use the entity shader
    }

    private void DrawGrid(IShaderManager shaderManager)
    {
        float gridSize = 10f;
        float step = 1f;
        var gridVerts = new System.Collections.Generic.List<float>();

        for (float x = -gridSize; x <= gridSize; x += step)
        {
            gridVerts.Add(x); gridVerts.Add(-0.01f); gridVerts.Add(-gridSize);
            gridVerts.Add(x); gridVerts.Add(-0.01f); gridVerts.Add(gridSize);
        }
        for (float z = -gridSize; z <= gridSize; z += step)
        {
            gridVerts.Add(-gridSize); gridVerts.Add(-0.01f); gridVerts.Add(z);
            gridVerts.Add(gridSize);  gridVerts.Add(-0.01f); gridVerts.Add(z);
        }

        float[] arr = gridVerts.ToArray();
        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, arr.Length * sizeof(float), arr, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        shaderManager.SetVec4("uColor", new Vector4(0.8f, 0.8f, 0.8f, 1f));
        GL.DrawArrays(PrimitiveType.Lines, 0, arr.Length / 3);

        GL.BindVertexArray(0);
        GL.DeleteBuffer(vbo);
    }

    private float[] BuildViewMatrix(ICamera camera)
    {
        // Identity matrix with camera position offset
        float[] m = new float[16];
        m[0] = 1; m[5] = 1; m[10] = 1; m[15] = 1;
        m[12] = -camera.Position.X;
        m[13] = -camera.Position.Y;
        m[14] = -camera.Position.Z;
        return m;
    }

    private float[] BuildProjectionMatrix()
    {
        // Isometric-style projection (simplified perspective)
        float fov = 45f * MathF.PI / 180f;
        float near = 0.1f;
        float far = 100f;
        float aspect = 16f / 9f;
        float f = 1.0f / MathF.Tan(fov / 2);
        float[] m = new float[16];
        m[0] = f / aspect;
        m[5] = f;
        m[10] = (far + near) / (near - far);
        m[11] = -1;
        m[14] = (2f * far * near) / (near - far);
        m[15] = 0;
        return m;
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(_cubeVAO);
        GL.DeleteBuffer(_cubeVBO);
        GL.DeleteBuffer(_cubeEBO);
    }
}
