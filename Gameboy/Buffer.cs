using System;
using Silk.NET.OpenGL;

namespace Gameboy;

public class VertexBuffer : Buffer<float>
{
    public VertexBuffer(GL gl, float[]? data = null)
        : base(gl, GLEnum.ArrayBuffer, data) { }
}

public class IndexBuffer : Buffer<uint>
{
    unsafe public IndexBuffer(GL gl, uint[]? data = null)
        : base(gl, GLEnum.ElementArrayBuffer, data) { }

    override public void FeedData(Span<uint> data, nint offset = 0)
    {
        Count = (uint)data.Length;
        base.FeedData(data, offset);
    }

    public uint Count { get; private set; }
}

public abstract class Buffer<T> : IDisposable where T : unmanaged
{
    protected uint id;
    protected GLEnum target;
    private readonly GL gl;

    unsafe public Buffer(GL gl, GLEnum target, T[]? data = null)
    {
        this.gl = gl;
        this.target = target;

        id = gl.CreateBuffer();
        Bind();

        if (data is not null)
        {
            FeedData(data);
        }
    }

    private nuint currentLength = 0;

    unsafe public virtual void FeedData(Span<T> data, nint offset = 0)
    {
        var lengthOfData = (nuint)(sizeof(T) * data.Length);
        fixed (void* dataPointer = data)
        {
            if (((nuint)offset) + lengthOfData < currentLength)
            {
                gl.BufferSubData(target, offset, lengthOfData, dataPointer);
            }
            else
            {
                gl.BufferData(target, lengthOfData, dataPointer, GLEnum.StaticDraw);
            }
        }
        currentLength = lengthOfData;
    }

    public void Bind()
    {
        gl.BindBuffer(target, id);
    }

    public void Unbind()
    {
        gl.BindBuffer(target, 0);
    }

    public void Dispose()
    {
        gl.DeleteBuffers(1, id);
    }
}
