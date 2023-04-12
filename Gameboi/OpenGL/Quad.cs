namespace Gameboi.OpenGL;

public struct Quad
{
    public const int Size = 16;
    public const int SizeInBytes = 16 * sizeof(float);
    public float[] verticesData;

    public Quad(float xStart, float yStart, float xEnd, float yEnd, float textureXstart, float textureYstart, float textureXend, float textureYend)
    {
        verticesData = new float[] {
            xStart, yStart, textureXstart, textureYstart,
            xEnd  , yStart, textureXend  , textureYstart,
            xEnd  , yEnd  , textureXend  , textureYend  ,
            xStart, yEnd  , textureXstart, textureYend  ,
        };
    }
}
