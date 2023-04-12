using System;
using System.Collections.Generic;
using Gameboi.Extensions;
using Gameboi.Graphics;
using Silk.NET.OpenGL;

namespace Gameboi.OpenGL;

public sealed class UiLayer : IDisposable
{
    private static readonly byte[] fontTextureData = new byte[]{
        0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000,
        0b_00011000, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00111100, 0b_00111100, 0b_00011100, 0b_00100100, 0b_00011100, 0b_00001110, 0b_00100100, 0b_00100000, 0b_00100010, 0b_00100010, 0b_00011100, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00011100, 0b_00111100, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00111110, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00001000, 0b_00100000, 0b_00001000, 0b_00001110, 0b_00011110, 0b_00011110, 0b_00111100, 0b_00010000, 0b_00111100, 0b_00111100, 0b_00011000, 0b_01111110, 0b_00011100, 0b_01111110, 0b_00111100, 0b_00111100,
        0b_00100100, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00100010, 0b_00100100, 0b_00001000, 0b_00000100, 0b_00101000, 0b_00100000, 0b_00110110, 0b_00110010, 0b_00100010, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100010, 0b_00001000, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00010100, 0b_00100010, 0b_00000100, 0b_00000000, 0b_00000000, 0b_00010000, 0b_00010000, 0b_00010000, 0b_00001000, 0b_00010001, 0b_00010000, 0b_00000010, 0b_01000010, 0b_00110000, 0b_01000010, 0b_01000010, 0b_00101000, 0b_01000000, 0b_00100000, 0b_00000010, 0b_01000010, 0b_01000010,
        0b_00100100, 0b_00111000, 0b_00100000, 0b_00100100, 0b_00111100, 0b_00111100, 0b_00100000, 0b_00111100, 0b_00001000, 0b_00000100, 0b_00110000, 0b_00100000, 0b_00101010, 0b_00101010, 0b_00100010, 0b_00111000, 0b_00100010, 0b_00111000, 0b_00010000, 0b_00001000, 0b_00100010, 0b_00100010, 0b_00101010, 0b_00001000, 0b_00010100, 0b_00001000, 0b_00000000, 0b_00000000, 0b_00111000, 0b_00100000, 0b_00001000, 0b_00001000, 0b_00000100, 0b_00010000, 0b_00000010, 0b_01000110, 0b_00010000, 0b_00000010, 0b_00000010, 0b_01001000, 0b_01000000, 0b_01000000, 0b_00000100, 0b_01000010, 0b_01000010,
        0b_00111100, 0b_00100100, 0b_00100000, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00101110, 0b_00100100, 0b_00001000, 0b_00000100, 0b_00101000, 0b_00100000, 0b_00100010, 0b_00100110, 0b_00100010, 0b_00100000, 0b_00100010, 0b_00101000, 0b_00001100, 0b_00001000, 0b_00100010, 0b_00010100, 0b_00101010, 0b_00010100, 0b_00001000, 0b_00010000, 0b_00000000, 0b_00000000, 0b_00010000, 0b_00100000, 0b_00001000, 0b_00001000, 0b_00001000, 0b_00010000, 0b_00000010, 0b_01001010, 0b_00010000, 0b_00000100, 0b_00011100, 0b_11111110, 0b_01111100, 0b_01111100, 0b_00001000, 0b_00111100, 0b_00111110,
        0b_00100100, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00100010, 0b_00100100, 0b_00001000, 0b_00100100, 0b_00100100, 0b_00100000, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100000, 0b_00100110, 0b_00100100, 0b_00100010, 0b_00001000, 0b_00100010, 0b_00010100, 0b_00101010, 0b_00100010, 0b_00001000, 0b_00100000, 0b_00001000, 0b_00001000, 0b_00101000, 0b_00010000, 0b_00010000, 0b_00000000, 0b_00001000, 0b_00010000, 0b_00000010, 0b_01010010, 0b_00010000, 0b_00001000, 0b_00000010, 0b_00001000, 0b_00000010, 0b_01000010, 0b_00010000, 0b_01000010, 0b_00000010,
        0b_00100100, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00111100, 0b_00100000, 0b_00011100, 0b_00100100, 0b_00011100, 0b_00011000, 0b_00100100, 0b_00111100, 0b_00100010, 0b_00100010, 0b_00011100, 0b_00100000, 0b_00011110, 0b_00100100, 0b_00011100, 0b_00001000, 0b_00011100, 0b_00001000, 0b_00010100, 0b_00100010, 0b_00001000, 0b_00111110, 0b_00001000, 0b_00001000, 0b_00000000, 0b_00001000, 0b_00100000, 0b_00001000, 0b_00000000, 0b_00010000, 0b_00000010, 0b_01100010, 0b_00010000, 0b_00010000, 0b_01000010, 0b_00001000, 0b_00000010, 0b_01000010, 0b_00100000, 0b_01000010, 0b_00000100,
        0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000001, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00010000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00001000, 0b_00011110, 0b_00011110, 0b_00111100, 0b_00111100, 0b_01111110, 0b_00111100, 0b_00001000, 0b_01111100, 0b_00111100, 0b_00100000, 0b_00111100, 0b_00011000,
    };

    private static readonly int fontSymbols = fontTextureData.Length / 8;
    private const int fontHeight = 8;
    private const int fontWidth = 8;

    private readonly GL gl;

    private readonly VertexArray vertexArray;
    private readonly VertexBuffer vertexBuffer;
    private readonly IndexBuffer indexBuffer;
    private readonly Texture fontTexture;
    private readonly Shaders fontShaders;

    private const int vertexCapacity = 20_000;
    private const int indexCapacity = 10_000;

    private readonly List<float> vertices = new();
    private int QuadCount => vertices.Count / 16;

    private readonly List<uint> indices = new();

    public UiLayer(GL gl)
    {
        this.gl = gl;

        vertexArray = new VertexArray(gl);
        vertexBuffer = new VertexBuffer(gl, vertexCapacity);
        vertexArray.AddBuffer(vertexBuffer);

        indexBuffer = new IndexBuffer(gl, indexCapacity);
        fontTexture = new Texture(gl, 1, null, (uint)(fontWidth * fontSymbols), fontHeight);
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

    private readonly float fontWidthUnit = 1f / fontSymbols;

    private readonly Dictionary<int, (bool, int, uint[])> loadedText = new();
    private int currentId = 0;

    public int FillScreen()
    {
        var newVertices = new float[]{
            -1, -1, 0, 0,
            1, -1, 0, 0,
            1, 1, 0, 0,
            -1, 1, 0, 0,
        };

        var newIndices = GetIndicesForQuad((uint)QuadCount);

        var id = currentId;
        loadedText[id] = (true, indices.Count, newIndices);
        currentId += 1;

        vertexBuffer.FeedSubData(newVertices, vertices.Count * sizeof(float));
        vertices.AddRange(newVertices);

        indexBuffer.FeedSubData(newIndices, indices.Count * sizeof(uint));
        indices.AddRange(newIndices);

        return id;
    }

    public int ShowText(string text, int row, int column)
    {
        var id = CreateText(text, row, column);
        ShowText(id);
        return id;
    }

    public int CreateText(string text, int row, int column)
    {
        var yStart = 1f - (tileHeight * row);
        var yEnd = yStart - tileHeight;

        var xStart = -1f + (tileWidth * column);
        var xEnd = xStart + tileWidth;

        var currentQuadCount = (uint)QuadCount;

        var newVertices = new List<float>();
        var newIndices = new List<uint>();

        foreach (var character in text.ToLower())
        {
            newIndices.AddRange(GetIndicesForQuad(currentQuadCount));


            var charIndex = character switch
            {
                '.' => 26,
                ',' => 27,
                '*' => 28,
                '(' => 29,
                ')' => 30,
                '!' => 31,
                '?' => 32,
                '[' => 33,
                ']' => 34,
                '0' => 35,
                '1' => 36,
                '2' => 37,
                '3' => 38,
                '4' => 39,
                '5' => 40,
                '6' => 41,
                '7' => 42,
                '8' => 43,
                '9' => 44,
                _ => character - 'a'
            };
            var fontXstart = fontWidthUnit * charIndex;

            var fontXEnd = fontXstart + fontWidthUnit;
            newVertices.AddRange(new float[]{
            //  x, y, tx, ty
                xStart, yStart, fontXstart, 0f,
                xEnd, yStart, fontXEnd, 0f,
                xEnd, yEnd, fontXEnd, 1f,
                xStart, yEnd, fontXstart, 1f,
            });

            xStart += tileWidth;
            xEnd += tileWidth;
            currentQuadCount += 1;
        }

        var indicesArray = newIndices.ToArray();
        // loadedText.Add(currentId, (false, indices.Count, indicesArray));
        loadedText.Add(currentId, (false, -1, indicesArray));

        vertexBuffer.FeedSubData(newVertices.ToArray(), vertices.Count * sizeof(float));
        // indexBuffer.FeedSubData(indicesArray, indices.Count * sizeof(uint));

        vertices.AddRange(newVertices);
        // indices.AddRange(newIndices);

        var id = currentId;
        currentId += 1;
        return id;
    }

    public void ShowText(int id)
    {
        var (isShowing, _, data) = loadedText[id];
        if (isShowing is false)
        {
            indexBuffer.FeedSubData(data, indices.Count * sizeof(uint));
            loadedText[id] = (true, indices.Count, data);
            indices.AddRange(data);
        }
    }

    public void HideText(int id)
    {
        var (isShowing, start, data) = loadedText[id];
        if (isShowing)
        {
            indices.RemoveRange(start, data.Length);
            indexBuffer.FeedSubData(indices.ToArray(), 0);
            loadedText[id] = (false, start, data);
        }
    }

    public void RemoveText(int id)
    {
        var (isShowing, _, data) = loadedText[id];
        if (isShowing)
        {
            vertices.RemoveRange((int)data[0] * 4, data.Length / 6 * 16);
            vertexBuffer.FeedSubData(vertices.ToArray(), 0);

            indices.Clear();
            for (var i = 0; i < QuadCount; i++)
            {
                indices.AddRange(GetIndicesForQuad((uint)i));
            }
            indexBuffer.FeedSubData(indices.ToArray(), 0);
            loadedText.Remove(id);
        }
    }

    private static uint[] GetIndicesForQuad(uint quadNr)
    {
        return new uint[]{
            (quadNr * 4) + 0,
            (quadNr * 4) + 1,
            (quadNr * 4) + 2,
            (quadNr * 4) + 2,
            (quadNr * 4) + 3,
            (quadNr * 4) + 0,
        };
    }


    private Rgba[] ProcessFontData()
    {
        var colors = new Rgba[fontWidth * fontHeight * fontSymbols];
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
