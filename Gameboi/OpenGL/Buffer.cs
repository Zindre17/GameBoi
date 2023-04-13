using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;

namespace Gameboi.OpenGL;

public class VertexBuffer : Buffer<float>
{
    public VertexBuffer(GL gl, float[]? data = null)
        : base(gl, GLEnum.ArrayBuffer, data) { }

    public VertexBuffer(GL gl, int capacity)
        : base(gl, GLEnum.ArrayBuffer, capacity) { }

    public void FeedSubData(IEnumerable<Quad> quads, int offset)
    {
        FeedSubData(quads.SelectMany(q => q.verticesData).ToArray(), offset);
    }

    public void FeedSubData(IEnumerable<ColoredQuad> quads, int offset)
    {
        FeedSubData(quads.SelectMany(q => q.verticesData).ToArray(), offset);
    }
}

public class IndexBuffer : Buffer<uint>
{
    public IndexBuffer(GL gl, uint[]? data = null)
       : base(gl, GLEnum.ElementArrayBuffer, data) { }

    public IndexBuffer(GL gl, int capacity)
        : base(gl, GLEnum.ElementArrayBuffer, capacity) { }

    override public void FeedData(Span<uint> data)
    {
        Count = (uint)data.Length;
        base.FeedData(data);
    }

    public override void FeedSubData(Span<uint> data, int offset)
    {
        Count = (uint)((offset / sizeof(uint)) + data.Length);
        base.FeedSubData(data, offset);
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

        if (data is not null)
        {
            FeedData(data);
        }
    }

    unsafe public Buffer(GL gl, GLEnum target, int capacity)
    {
        this.gl = gl;
        this.target = target;

        id = gl.GenBuffer();
        Bind();

        gl.BufferData(target, (nuint)capacity, null, GLEnum.StaticDraw);
        currentCapacity = capacity;
    }

    private int currentCapacity;

    unsafe public virtual void FeedData(Span<T> data)
    {
        Bind();
        var lengthOfData = sizeof(T) * data.Length;
        fixed (void* dataPointer = data)
        {
            gl.BufferData(target, (nuint)lengthOfData, dataPointer, GLEnum.StaticDraw);
        }
        currentCapacity = lengthOfData;
    }

    unsafe public virtual void FeedSubData(Span<T> data, int offset)
    {
        Bind();
        var lengthOfData = sizeof(T) * data.Length;
        if (lengthOfData + offset > currentCapacity)
        {
            throw new Exception($"Buffer overflow:\n\tcurrent capacity:{currentCapacity}\n\tneeded capacity:{lengthOfData + offset}");
        }
        fixed (void* dataPointer = data)
        {
            gl.BufferSubData(target, offset, (nuint)lengthOfData, dataPointer);
        }
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
        GC.SuppressFinalize(this);
    }
}
