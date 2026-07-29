using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Djurspel.Graphics;

public class ShaderManager : IShaderManager
{
    private readonly Dictionary<string, ShaderProgram> _programs = new();
    private bool _disposed;

    public ShaderProgram Load(string name, string vertexSource, string fragmentSource, string? geometrySource = null)
    {
        if (_programs.TryGetValue(name, out var existing))
            return existing;

        int vs = CompileShader(ShaderType.VertexShader, vertexSource);
        int fs = CompileShader(ShaderType.FragmentShader, fragmentSource);
        int gs = 0;
        if (geometrySource != null)
            gs = CompileShader(ShaderType.GeometryShader, geometrySource);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vs);
        GL.AttachShader(program, fs);
        if (gs != 0)
            GL.AttachShader(program, gs);

        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            throw new IOException($"Shader linking failed for '{name}': {infoLog}");
        }

        GL.DetachShader(program, vs);
        GL.DetachShader(program, fs);
        if (gs != 0)
            GL.DetachShader(program, gs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        if (gs != 0)
            GL.DeleteShader(gs);

        var programObj = new ShaderProgram { GlProgramId = program, Name = name };
        CacheUniforms(program, programObj);
        _programs[name] = programObj;
        return programObj;
    }

    public ShaderProgram? Get(string name) => _programs.TryGetValue(name, out var prog) ? prog : null;

    public void Bind(ShaderProgram shader)
    {
        if (shader == null) return;
        GL.UseProgram(shader.GlProgramId);
    }

    public void SetFloat(string name, float value)
    {
        int loc = GL.GetUniformLocation(GetCurrentProgram(), name);
        if (loc >= 0) GL.Uniform1(loc, value);
    }

    public void SetVec3(string name, Vector3 value)
    {
        int loc = GL.GetUniformLocation(GetCurrentProgram(), name);
        if (loc >= 0) GL.Uniform3(loc, value.X, value.Y, value.Z);
    }

    public void SetVec4(string name, Vector4 value)
    {
        int loc = GL.GetUniformLocation(GetCurrentProgram(), name);
        if (loc >= 0) GL.Uniform4(loc, value.X, value.Y, value.Z, value.W);
    }

    public void SetMat4(string name, float[] matrix)
    {
        if (matrix == null || matrix.Length != 16)
            throw new ArgumentException("Matrix must be 16 floats", nameof(matrix));

        int loc = GL.GetUniformLocation(GetCurrentProgram(), name);
        if (loc >= 0) GL.UniformMatrix4(loc, 1, false, matrix);
    }

    public void SetInt(string name, int value)
    {
        int loc = GL.GetUniformLocation(GetCurrentProgram(), name);
        if (loc >= 0) GL.Uniform1(loc, value);
    }

    private int GetCurrentProgram()
    {
        GL.GetInteger(GetPName.CurrentProgram, out int prog);
        return prog;
    }

    private int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new IOException($"Shader compilation failed ({type}): {infoLog}");
        }

        return shader;
    }

    private void CacheUniforms(int program, ShaderProgram programObj)
    {
        GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int count);

        for (int i = 0; i < count; i++)
        {
            int nameLength = 0;
            int size = 0;
            ActiveUniformType type = ActiveUniformType.Float;
            string? uniformName = null;
            GL.GetActiveUniform(program, i, 512, out nameLength, out size, out type, out uniformName);
            if (string.IsNullOrEmpty(uniformName))
                continue;

            int loc = GL.GetUniformLocation(program, uniformName);
            if (loc >= 0)
                programObj.UniformLocations[uniformName] = loc;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var prog in _programs)
            GL.DeleteProgram(prog.Value.GlProgramId);
        _programs.Clear();
    }
}
