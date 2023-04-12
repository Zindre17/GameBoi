using System;
using System.Collections.Generic;
using System.Linq;
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

    private readonly List<Quad> quads = new();
    private readonly List<uint> indices = new();
    private readonly List<Quad> orderedVisibleQuads = new();

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

    private readonly Dictionary<int, (bool, Quad, int)> loadedText = new();
    private int currentId = 0;

    public int FillScreen()
    {
        var quad = new Quad(-1, -1, 1, 1, 0, 0, 0, 0);
        var newIndices = GetIndicesForQuad(quads.Count);

        var id = currentId;
        loadedText[id] = (true, quad, 1);
        currentId += 1;

        vertexBuffer.FeedSubData(quad.verticesData, quads.Count * Quad.SizeInBytes);
        quads.Add(quad);

        indexBuffer.FeedSubData(newIndices, indices.Count * sizeof(uint));
        indices.AddRange(newIndices);
        orderedVisibleQuads.Add(quad);

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

        var newQuads = new List<Quad>();

        foreach (var character in text.ToLower())
        {
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
            var fontXend = fontXstart + fontWidthUnit;

            newQuads.Add(new Quad(xStart, yStart, xEnd, yEnd, fontXstart, 0, fontXend, 1));

            xStart += tileWidth;
            xEnd += tileWidth;
        }

        loadedText.Add(currentId, (false, newQuads[0], newQuads.Count));

        vertexBuffer.FeedSubData(newQuads, quads.Count * Quad.SizeInBytes);

        quads.AddRange(newQuads);

        var id = currentId;
        currentId += 1;
        return id;
    }

    public void ShowText(int id)
    {
        ShowText(new int[] { id });
    }

    public void ShowText(IEnumerable<int> ids)
    {
        var allNewIndices = new List<uint>();
        foreach (var id in ids)
        {
            var (isShowing, firstQuad, quadCount) = loadedText[id];
            if (isShowing is false)
            {
                var firstIndex = quads.IndexOf(firstQuad);
                var newIndices = GetIndicesForQuads(firstIndex, quadCount);
                loadedText[id] = (true, firstQuad, quadCount);
                orderedVisibleQuads.AddRange(quads.Skip(firstIndex).Take(quadCount));
                allNewIndices.AddRange(newIndices);
            }
        }
        indexBuffer.FeedSubData(allNewIndices.ToArray(), indices.Count * sizeof(uint));
        indices.AddRange(allNewIndices);
    }

    public void HideText(int id)
    {
        HideText(new int[] { id });
    }

    public void HideText(IEnumerable<int> ids)
    {
        foreach (var id in ids)
        {
            var (isShowing, firstQuad, quadCount) = loadedText[id];
            if (isShowing)
            {
                var firstIndex = orderedVisibleQuads.IndexOf(firstQuad);
                indices.RemoveRange(firstIndex * 6, quadCount * 6);
                orderedVisibleQuads.RemoveRange(firstIndex, quadCount);
                loadedText[id] = (false, firstQuad, quadCount);
            }
        }
        indexBuffer.FeedSubData(indices.ToArray(), 0);
    }

    public void RemoveText(int id)
    {
        RemoveText(new int[] { id });
    }

    public void RemoveText(IEnumerable<int> ids)
    {
        foreach (var id in ids)
        {
            var (isShowing, firstQuad, quadCount) = loadedText[id];
            var firstIndex = quads.IndexOf(firstQuad);
            quads.RemoveRange(firstIndex, quadCount);
            if (isShowing)
            {
                var index = orderedVisibleQuads.IndexOf(firstQuad);
                orderedVisibleQuads.RemoveRange(index, quadCount);
            }
            loadedText.Remove(id);
        }
        vertexBuffer.FeedSubData(quads, 0);

        indices.Clear();
        foreach (var quad in orderedVisibleQuads)
        {
            var index = quads.IndexOf(quad);
            indices.AddRange(GetIndicesForQuad(index));
        }
        indexBuffer.FeedSubData(indices.ToArray(), 0);
    }

    private static uint[] GetIndicesForQuads(int startQuadNr, int count)
    {
        var indices = new List<uint>();
        for (var i = startQuadNr; i < startQuadNr + count; i++)
        {
            indices.AddRange(GetIndicesForQuad(i));
        }
        return indices.ToArray();
    }

    private static uint[] GetIndicesForQuad(int quadNr)
    {
        return new uint[]{
            (uint)(quadNr * 4) + 0,
            (uint)(quadNr * 4) + 1,
            (uint)(quadNr * 4) + 2,
            (uint)(quadNr * 4) + 2,
            (uint)(quadNr * 4) + 3,
            (uint)(quadNr * 4) + 0,
        };
    }

    private static Rgba[] ProcessFontData()
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
