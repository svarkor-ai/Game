using Djurspel.Core;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System;

namespace Djurspel.Graphics;

/// <summary>
/// Generates primitive meshes (cube, sphere, cylinder, plane) as MeshAsset objects.
/// Vertices are generated on CPU; VAO upload happens at build time.
/// </summary>
public class PrimitiveMeshFactory : IPrimitiveMeshFactory
{
    public MeshAsset CreateCube()
    {
        float[] vertices = {
            // Front face
            -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f,  0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,
            // Back face
            -0.5f, -0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f, -0.5f, -0.5f,
            // Left face
            -0.5f, -0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,  -0.5f,  0.5f,  0.5f,  -0.5f, -0.5f,  0.5f,
            // Right face
             0.5f, -0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f,  0.5f,  0.5f,   0.5f, -0.5f,  0.5f,
            // Top face
            -0.5f,  0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f,  0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,
            // Bottom face
            -0.5f, -0.5f, -0.5f,  -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f, -0.5f, -0.5f
        };

        float[] normals = {
            0,0,1, 0,0,1, 0,0,1, 0,0,1,
            0,0,-1, 0,0,-1, 0,0,-1, 0,0,-1,
            -1,0,0, -1,0,0, -1,0,0, -1,0,0,
            1,0,0, 1,0,0, 1,0,0, 1,0,0,
            0,1,0, 0,1,0, 0,1,0, 0,1,0,
            0,-1,0, 0,-1,0, 0,-1,0, 0,-1,0
        };

        float[] uvs = {
            0,0, 1,0, 1,1, 0,1,
            0,0, 1,0, 1,1, 0,1,
            0,0, 1,0, 1,1, 0,1,
            0,0, 1,0, 1,1, 0,1,
            0,0, 1,0, 1,1, 0,1,
            0,0, 1,0, 1,1, 0,1
        };

        int[] indices = {
            0,1,2, 2,3,0,   // front
            4,5,6, 6,7,4,   // back
            8,9,10,10,11,8,  // left
            12,13,14,14,15,12, // right
            16,17,18,18,19,16, // top
            20,21,22,22,23,20  // bottom
        };

        return CreateMeshAsset("cube", vertices, normals, uvs, indices);
    }

    public MeshAsset CreateSphere(int segments = 16)
    {
        var verts = new System.Collections.Generic.List<float>();
        var nrm = new System.Collections.Generic.List<float>();
        var uv = new System.Collections.Generic.List<float>();
        var idx = new System.Collections.Generic.List<int>();

        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = (float)lat * MathF.PI / segments;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = (float)lon * 2 * MathF.PI / segments;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi * sinTheta;
                float y = cosTheta;
                float z = sinPhi * sinTheta;

                verts.Add(x * 0.5f); verts.Add(y * 0.5f); verts.Add(z * 0.5f);
                nrm.Add(x); nrm.Add(y); nrm.Add(z);
                uv.Add((float)lon / segments); uv.Add((float)lat / segments);
            }
        }

        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = lat * (segments + 1) + lon;
                int second = first + segments + 1;
                idx.Add(first); idx.Add(second); idx.Add(first + 1);
                idx.Add(second); idx.Add(second + 1); idx.Add(first + 1);
            }
        }

        return CreateMeshAsset("sphere",
            verts.ToArray(), nrm.ToArray(), uv.ToArray(), idx.ToArray());
    }

    public MeshAsset CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 16)
    {
        var verts = new System.Collections.Generic.List<float>();
        var nrm = new System.Collections.Generic.List<float>();
        var uv = new System.Collections.Generic.List<float>();
        var idx = new System.Collections.Generic.List<int>();

        // Top cap center
        verts.Add(0); verts.Add(height * 0.5f); verts.Add(0);
        nrm.Add(0); nrm.Add(1); nrm.Add(0);
        uv.Add(0.5f); uv.Add(0.5f);
        int topCenter = verts.Count / 3 - 1;

        // Bottom cap center
        verts.Add(0); verts.Add(-height * 0.5f); verts.Add(0);
        nrm.Add(0); nrm.Add(-1); nrm.Add(0);
        uv.Add(0.5f); uv.Add(0.5f);
        int bottomCenter = verts.Count / 3 - 1;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i * 2 * MathF.PI / segments;
            float x = MathF.Cos(angle);
            float z = MathF.Sin(angle);

            // Top rim
            verts.Add(x * radius); verts.Add(height * 0.5f); verts.Add(z * radius);
            nrm.Add(x); nrm.Add(0); nrm.Add(z);
            uv.Add((float)i / segments); uv.Add(0);

            // Bottom rim
            verts.Add(x * radius); verts.Add(-height * 0.5f); verts.Add(z * radius);
            nrm.Add(x); nrm.Add(0); nrm.Add(z);
            uv.Add((float)i / segments); uv.Add(1);
        }

        int topRimStart = 1;
        int bottomRimStart = 1 + segments + 1;

        // Side triangles
        for (int i = 0; i < segments; i++)
        {
            idx.Add(topRimStart + i);
            idx.Add(bottomRimStart + i);
            idx.Add(topRimStart + i + 1);

            idx.Add(bottomRimStart + i);
            idx.Add(bottomRimStart + i + 1);
            idx.Add(topRimStart + i + 1);
        }

        // Top cap triangles
        for (int i = 0; i < segments; i++)
        {
            idx.Add(topCenter);
            idx.Add(topRimStart + i + 1);
            idx.Add(topRimStart + i);
        }

        // Bottom cap triangles
        for (int i = 0; i < segments; i++)
        {
            idx.Add(bottomCenter);
            idx.Add(bottomRimStart + i);
            idx.Add(bottomRimStart + i + 1);
        }

        return CreateMeshAsset("cylinder",
            verts.ToArray(), nrm.ToArray(), uv.ToArray(), idx.ToArray());
    }

    public MeshAsset CreatePlane(float width = 1.0f, float depth = 1.0f)
    {
        float hw = width * 0.5f;
        float hd = depth * 0.5f;

        float[] vertices = {
            -hw, 0, -hd,   hw, 0, -hd,   hw, 0,  hd,  -hw, 0,  hd
        };
        float[] normals = {
            0,1,0, 0,1,0, 0,1,0, 0,1,0
        };
        float[] uvs = {
            0,0, 1,0, 1,1, 0,1
        };
        int[] indices = { 0, 1, 2, 2, 3, 0 };

        return CreateMeshAsset("plane", vertices, normals, uvs, indices);
    }

    public MeshAsset CreateIsometricTile(float width, float depth, float height)
    {
        float hw = width * 0.5f;
        float hd = depth * 0.5f;
        float hh = height * 0.5f;

        // 6 faces for isometric tile
        var verts = new System.Collections.Generic.List<float>();
        var nrm = new System.Collections.Generic.List<float>();
        var uv = new System.Collections.Generic.List<float>();
        var idx = new System.Collections.Generic.List<int>();

        // Bottom face
        verts.Add(-hw); verts.Add(-hh); verts.Add(-hd); verts.Add(hw); verts.Add(-hh); verts.Add(-hd); verts.Add(hw); verts.Add(-hh); verts.Add(hd); verts.Add(-hw); verts.Add(-hh); verts.Add(hd);
        nrm.Add(0); nrm.Add(-1); nrm.Add(0); nrm.Add(0); nrm.Add(-1); nrm.Add(0); nrm.Add(0); nrm.Add(-1); nrm.Add(0); nrm.Add(0); nrm.Add(-1); nrm.Add(0);
        uv.Add(0); uv.Add(0); uv.Add(1); uv.Add(0); uv.Add(1); uv.Add(1); uv.Add(0); uv.Add(1);
        int base0 = verts.Count / 3 - 4;
        idx.Add(base0); idx.Add(base0 + 1); idx.Add(base0 + 2); idx.Add(base0 + 2); idx.Add(base0 + 3); idx.Add(base0);

        // Top face
        verts.Add(-hw); verts.Add(hh); verts.Add(-hd); verts.Add(hw); verts.Add(hh); verts.Add(-hd); verts.Add(hw); verts.Add(hh); verts.Add(hd); verts.Add(-hw); verts.Add(hh); verts.Add(hd);
        nrm.Add(0); nrm.Add(1); nrm.Add(0); nrm.Add(0); nrm.Add(1); nrm.Add(0); nrm.Add(0); nrm.Add(1); nrm.Add(0); nrm.Add(0); nrm.Add(1); nrm.Add(0);
        uv.Add(0); uv.Add(0); uv.Add(1); uv.Add(0); uv.Add(1); uv.Add(1); uv.Add(0); uv.Add(1);
        int base1 = verts.Count / 3 - 4;
        idx.Add(base1); idx.Add(base1 + 1); idx.Add(base1 + 2); idx.Add(base1 + 2); idx.Add(base1 + 3); idx.Add(base1);

        // Front face
        verts.Add(-hw); verts.Add(-hh); verts.Add(hd); verts.Add(hw); verts.Add(-hh); verts.Add(hd); verts.Add(hw); verts.Add(hh); verts.Add(hd); verts.Add(-hw); verts.Add(hh); verts.Add(hd);
        nrm.Add(0); nrm.Add(0); nrm.Add(1); nrm.Add(0); nrm.Add(0); nrm.Add(1); nrm.Add(0); nrm.Add(0); nrm.Add(1); nrm.Add(0); nrm.Add(0); nrm.Add(1);
        uv.Add(0); uv.Add(0); uv.Add(1); uv.Add(0); uv.Add(1); uv.Add(1); uv.Add(0); uv.Add(1);
        int base2 = verts.Count / 3 - 4;
        idx.Add(base2); idx.Add(base2 + 1); idx.Add(base2 + 2); idx.Add(base2 + 2); idx.Add(base2 + 3); idx.Add(base2);

        return CreateMeshAsset("isometric_tile",
            verts.ToArray(), nrm.ToArray(), uv.ToArray(), idx.ToArray());
    }

    private static MeshAsset CreateMeshAsset(string name, float[] vertices, float[] normals, float[] uvs, int[] indices)
    {
        // Upload to GPU
        int vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        int nbo = GL.GenBuffer();
        int uvo = GL.GenBuffer();
        int ebo = GL.GenBuffer();

         GL.BindVertexArray(vao);

        // Vertices
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

        // Normals
        GL.BindBuffer(BufferTarget.ArrayBuffer, nbo);
        GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * sizeof(float), normals, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);

        // UVs
        GL.BindBuffer(BufferTarget.ArrayBuffer, uvo);
        GL.BufferData(BufferTarget.ArrayBuffer, uvs.Length * sizeof(float), uvs, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);

        // Indices
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);

        return new MeshAsset
        {
            Name = name,
            VaoId = (int)vao,
            ElementCount = indices.Length,
            Vertices = vertices,
            Normals = normals,
            Uv = uvs,
            Indices = indices
        };
    }
}
