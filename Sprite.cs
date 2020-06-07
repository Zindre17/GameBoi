class Sprite
{
    private byte x;
    private byte y;
    private byte pattern;
    private byte flags;

    public Sprite(int block)
    {
        x = (byte)block;
        y = (byte)(block >> 8);
        pattern = (byte)(block >> 16);
        flags = (byte)(block >> 24);
    }
}