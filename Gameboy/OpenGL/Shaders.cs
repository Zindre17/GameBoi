using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Silk.NET.OpenGL;

namespace Gameboy.OpenGL;
public class Shaders : IDisposable
{
    readonly uint program;
    private readonly Dictionary<string, int> uniformLocations = new();
    private readonly GL gl;

    public Shaders(GL gl)
    {
        this.gl = gl;
        var shaders = FindShaders("./Gameboy/OpenGL/Basic.shader");

        program = gl.CreateProgram();
        var vert = gl.CreateShader(ShaderType.VertexShader);
        var frag = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(vert, shaders[0]);
        gl.ShaderSource(frag, shaders[1]);
        gl.CompileShader(vert);
        gl.CompileShader(frag);

        var info = gl.GetShaderInfoLog(vert);
        if (!string.IsNullOrEmpty(info))
        {
            Console.WriteLine(info);
        }
        info = gl.GetShaderInfoLog(frag);
        if (!string.IsNullOrEmpty(info))
        {
            Console.WriteLine(info);
        }

        gl.AttachShader(program, vert);
        gl.AttachShader(program, frag);
        gl.LinkProgram(program);

        gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status is 0)
        {
            Console.WriteLine(gl.GetProgramInfoLog(program));
        }

        // Cleanup of temporary resources
        gl.DetachShader(program, vert);
        gl.DetachShader(program, frag);
        gl.DeleteShader(vert);
        gl.DeleteShader(frag);
    }

    public void SetUniform(string name, int value)
    {
        gl.Uniform1(GetUniformLocation(name), value);
    }

    private int GetUniformLocation(string name)
    {
        if (!uniformLocations.TryGetValue(name, out var location))
        {
            location = gl.GetUniformLocation(program, name);
            if (location is -1)
            {
                throw new Exception("Did not find uniform");
            }
            uniformLocations[name] = location;
        };
        return location;
    }


    public void Bind()
    {
        gl.UseProgram(program);
    }

    public void Unbind()
    {
        gl.UseProgram(0);
    }

    public void Dispose()
    {
        gl.DeleteProgram(program);
    }

    private const string ShaderStart = "#shader";
    private const string VertexShader = "vertex";
    private const string FragmentShader = "fragment";

    private static string[] FindShaders(string filepath)
    {
        var sourceBuilders = new StringBuilder[2] { new(), new() };
        var mode = -1;

        var file = File.ReadLines(filepath);
        foreach (var line in file)
        {
            if (line.Contains(ShaderStart))
            {
                if (line.Contains(VertexShader))
                {
                    mode = 0;
                }
                else if (line.Contains(FragmentShader))
                {
                    mode = 1;
                }
            }
            else if (mode > -1)
            {
                sourceBuilders[mode].Append(line);
                sourceBuilders[mode].Append(Environment.NewLine);
            }
        }

        return sourceBuilders.Select(b => b.ToString()).ToArray();
    }
}
