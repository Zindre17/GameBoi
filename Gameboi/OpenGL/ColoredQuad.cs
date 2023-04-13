using Gameboi.Graphics;

namespace Gameboi.OpenGL;

public struct ColoredQuad
{
    public const int VertexFloatCount = 12;
    public const int Size = VertexFloatCount * 4;
    public const int SizeInBytes = Size * sizeof(float);
    public float[] verticesData;

    public ColoredQuad(float xStart, float yStart, float xEnd, float yEnd, float textureXstart, float textureYstart, float textureXend, float textureYend, Rgba foreground, Rgba background)
    {
        verticesData = new float[] {
            xStart, yStart, textureXstart, textureYstart, foreground.Red / 255f, foreground.Green / 255f, foreground.Blue / 255f, foreground.Alpha / 255f, background.Red / 255f, background.Green / 255f, background.Blue / 255f, background.Alpha / 255f,
            xEnd  , yStart, textureXend  , textureYstart, foreground.Red / 255f, foreground.Green / 255f, foreground.Blue / 255f, foreground.Alpha / 255f, background.Red / 255f, background.Green / 255f, background.Blue / 255f, background.Alpha / 255f,
            xEnd  , yEnd  , textureXend  , textureYend  , foreground.Red / 255f, foreground.Green / 255f, foreground.Blue / 255f, foreground.Alpha / 255f, background.Red / 255f, background.Green / 255f, background.Blue / 255f, background.Alpha / 255f,
            xStart, yEnd  , textureXstart, textureYend  , foreground.Red / 255f, foreground.Green / 255f, foreground.Blue / 255f, foreground.Alpha / 255f, background.Red / 255f, background.Green / 255f, background.Blue / 255f, background.Alpha / 255f,
        };
    }
}
