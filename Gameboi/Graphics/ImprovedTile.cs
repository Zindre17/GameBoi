namespace Gameboi.Graphics;

public readonly struct ImprovedTile
{
    private readonly byte[] data;
    private readonly int startAddress;

    public ImprovedTile(byte[] data, int startAddress)
    {
        this.data = data;
        this.startAddress = startAddress;
    }

    private const byte BytesPerRow = 2;

    public int GetColorIndex(int row, int column)
    {
        var rowStartOffset = (row * BytesPerRow) + startAddress;

        var firstByte = data[rowStartOffset];
        var secondByte = data[rowStartOffset + 1];

        var shift = 7 - column;
        var firstLayer = (firstByte >> shift) & 1;
        var secondLayer = (secondByte >> shift) & 1;

        return (secondLayer << 1) | firstLayer;
    }
}
