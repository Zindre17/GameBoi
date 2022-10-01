using System;
using Silk.NET.OpenGL;

namespace Gameboy;

public class Texture : IDisposable
{
    private readonly uint id;
    private readonly GLEnum target = GLEnum.Texture2D;
    private readonly GL gl;
    private readonly uint slot;

    private readonly uint width;
    private readonly uint height;

    unsafe public Texture(GL gl, uint slot, byte[]? textureData, uint width, uint height)
    {
        this.gl = gl;
        this.slot = slot;
        this.width = width;
        this.height = height;

        id = gl.GenTexture();
        Bind();

        gl.TexParameterI(target, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameterI(target, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        gl.TexParameterI(target, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameterI(target, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);

        if (textureData is null)
        {
            gl.TexImage2D(target, 0, (int)GLEnum.Rgba8, width, height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        }
        else
        {
            fixed (void* data = textureData)
            {
                gl.TexImage2D(target, 0, (int)GLEnum.Rgba8, width, height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, data);
            }
        }
    }

    unsafe public void FeedData<T>(T[] textureData) where T : unmanaged
    {
        Bind();
        fixed (void* data = textureData)
        {
            gl.TexSubImage2D(target, 0, 0, 0, width, height, GLEnum.Rgba, GLEnum.UnsignedByte, data);
        }
    }

    public void Bind()
    {
        gl.ActiveTexture((GLEnum)((int)GLEnum.Texture0 + slot));
        gl.BindTexture(target, id);
    }

    public void Unbind()
    {
        gl.BindTexture(target, 0);
    }

    public void Dispose()
    {
        gl.DeleteTexture(id);
    }
}
