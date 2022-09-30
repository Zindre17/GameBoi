using System;
using Silk.NET.OpenGL;

namespace Gameboy;

public class VertexArray : IDisposable
{
    private readonly uint id;
    private readonly GL gl;
    public VertexArray(GL gl)
    {
        this.gl = gl;
        id = gl.GenVertexArray();
        Bind();
    }

    unsafe public void AddBuffer(VertexBuffer buffer)
    {
        Bind();
        buffer.Bind();

        var stride = (uint)(sizeof(float) * 4);
        var glType = GLEnum.Float;

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(
            0,
            2,
            glType,
            false,
            stride,
            (void*)0
        );

        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(
            1,
            2,
            glType,
            false,
            stride,
            (void*)(2 * sizeof(float))
        );
    }

    public void Bind()
    {
        gl.BindVertexArray(id);
    }

    public void Unbind()
    {
        gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        gl.DeleteVertexArray(id);
    }

    private static GLEnum GetGlType(Type type)
    {
        if (type == typeof(float))
        {
            return GLEnum.Float;
        }
        else if (type == typeof(int))
        {
            return GLEnum.Int;
        }
        else if (type == typeof(uint))
        {
            return GLEnum.UnsignedInt;
        }
        else if (type == typeof(byte))
        {
            return GLEnum.Byte;
        }
        else
        {
            throw new ArgumentException("Unsupported type");
        }
    }
}
