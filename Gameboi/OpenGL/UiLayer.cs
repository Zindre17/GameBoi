using System;
using System.Collections.Generic;
using Gameboi.Extensions;
using Gameboi.Graphics;
using Silk.NET.OpenGL;

namespace Gameboi.OpenGL;

public sealed class UiLayer : IDisposable
{
    private readonly byte[] fontTextureData = new byte[]{
        0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000,
        0b_00011000, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00111100, 0b_00111100, 0b_00011100, 0b_00100100, 0b_00011100, 0b_00001110, 0b_00100100, 0b_00100000, 0b_00100010, 0b_00100010, 0b_00011100, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00011100, 0b_00111100, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00111110,
        0b_00100100, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00100010, 0b_00100100, 0b_00001000, 0b_00000100, 0b_00101000, 0b_00100000, 0b_00110110, 0b_00110010, 0b_00100010, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100010, 0b_00001000, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00010100, 0b_00100010, 0b_00000100,
        0b_00100100, 0b_00111000, 0b_00100000, 0b_00100100, 0b_00111100, 0b_00111100, 0b_00100000, 0b_00111100, 0b_00001000, 0b_00000100, 0b_00110000, 0b_00100000, 0b_00101010, 0b_00101010, 0b_00100010, 0b_00111000, 0b_00100010, 0b_00111000, 0b_00010000, 0b_00001000, 0b_00100010, 0b_00100010, 0b_00101010, 0b_00001000, 0b_00010100, 0b_00001000,
        0b_00111100, 0b_00100100, 0b_00100000, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00101110, 0b_00100100, 0b_00001000, 0b_00000100, 0b_00101000, 0b_00100000, 0b_00100010, 0b_00100110, 0b_00100010, 0b_00100000, 0b_00100010, 0b_00101000, 0b_00001100, 0b_00001000, 0b_00100010, 0b_00010100, 0b_00101010, 0b_00010100, 0b_00001000, 0b_00010000,
        0b_00100100, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00100010, 0b_00100100, 0b_00001000, 0b_00100100, 0b_00100100, 0b_00100000, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100000, 0b_00100110, 0b_00100100, 0b_00100010, 0b_00001000, 0b_00100010, 0b_00010100, 0b_00101010, 0b_00100010, 0b_00001000, 0b_00100000,
        0b_00100100, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00111100, 0b_00100000, 0b_00011100, 0b_00100100, 0b_00011100, 0b_00011000, 0b_00100100, 0b_00111100, 0b_00100010, 0b_00100010, 0b_00011100, 0b_00100000, 0b_00011110, 0b_00100100, 0b_00011100, 0b_00001000, 0b_00011100, 0b_00001000, 0b_00010100, 0b_00100010, 0b_00001000, 0b_00111110,
        0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000001, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000,
    };

    private readonly GL gl;

    private readonly VertexArray vertexArray;
    private readonly VertexBuffer vertexBuffer;
    private readonly IndexBuffer indexBuffer;
    private readonly Texture fontTexture;
    private readonly Shaders fontShaders;

    private int currentVertexBufferOffset = 0;
    private int currentIndexBufferOffset = 0;

    private const int vertexCapacity = 2_000;
    private const int indexCapacity = 1_000;

    public UiLayer(GL gl)
    {
        this.gl = gl;

        vertexArray = new VertexArray(gl);
        vertexBuffer = new VertexBuffer(gl, vertexCapacity);
        vertexArray.AddBuffer(vertexBuffer);

        indexBuffer = new IndexBuffer(gl, indexCapacity);
        fontTexture = new Texture(gl, 1, null, 8 * 26, 8);
        fontTexture.FeedData(ProcessFontData());

        fontShaders = new Shaders(gl, "OpenGL.Text.shader");
        fontShaders.SetUniform("Font", 1);
    }

    public void Dispose()
    {
        vertexArray.Dispose();
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        fontShaders.Dispose();
        fontTexture.Dispose();
    }

    public unsafe void Render()
    {
        vertexArray.Bind();
        vertexBuffer.Bind();
        indexBuffer.Bind();
        fontShaders.Bind();
        gl!.DrawElements(GLEnum.Triangles, indexBuffer.Count, GLEnum.UnsignedInt, null);
    }

    private const float heightUnit = 2f / 144;
    private const float widthUnit = 2f / 160;

    private const float tileHeight = heightUnit * 8;
    private const float tileWidth = widthUnit * 8;

    private const float fontWidthUnit = 1f / 26;

    public void ShowText(string text, int row, int column)
    {
        var yStart = 1f - (tileHeight * row);
        var yEnd = yStart - tileHeight;

        var xStart = -1f + (tileWidth * column);
        var xEnd = xStart + tileWidth;

        var vertices = new List<float>();
        var indices = new List<uint>();

        var currentVertexCount = (uint)currentVertexBufferOffset / (sizeof(float) * 4);

        foreach (var character in text.ToLower())
        {
            indices.AddRange(new uint[]{
               currentVertexCount + 0,
               currentVertexCount + 1,
               currentVertexCount + 2,
               currentVertexCount + 2,
               currentVertexCount + 3,
               currentVertexCount + 0,
            });

            var charIndex = character - 'a';
            var fontXstart = fontWidthUnit * charIndex;

            var fontXEnd = fontXstart + fontWidthUnit;
            vertices.AddRange(new float[]{
            //  x, y, tx, ty
                xStart, yStart, fontXstart, 0f,
                xEnd, yStart, fontXEnd, 0f,
                xEnd, yEnd, fontXEnd, 1f,
                xStart, yEnd, fontXstart, 1f,
            });

            xStart += tileWidth;
            xEnd += tileWidth;
            currentVertexCount += 4;
        }

        vertexBuffer.FeedSubData(vertices.ToArray(), currentVertexBufferOffset);
        currentVertexBufferOffset += vertices.Count * sizeof(float);

        indexBuffer.FeedSubData(indices.ToArray(), currentIndexBufferOffset);
        currentIndexBufferOffset += indices.Count * sizeof(uint);
    }

    private Rgba[] ProcessFontData()
    {
        var colors = new Rgba[8 * 8 * 26];
        for (var i = 0; i < fontTextureData.Length; i++)
        {
            var data = fontTextureData[i];
            for (var j = 0; j < 8; j++)
            {
                var index = 8 * i + j;
                colors[index] = new(data.IsBitSet(7 - j) ? Rgb.white : Rgb.darkGray);
            }
        }
        return colors;
    }
}
