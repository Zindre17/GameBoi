using System;
using static TileDataConstants;

class Tile : IMemoryRange
{
    private IMemory[] data = new IMemory[bytesPerTile];

    public Address Size => bytesPerTile;

    public IMemory this[Address address] { get => data[address]; set { } }

    public Tile()
    {
        for (int i = 0; i < bytesPerTile; i++)
            data[i] = new Register();
    }

    public Byte GetColorCode(Byte x, Byte y)
    {
        if (x > 7 || y > 7)
            throw new ArgumentOutOfRangeException("x and y must be lower than 8");

        Byte result = 0;

        Byte rowIndex = y * 2;
        if (data[rowIndex + 1].Read()[7 - x]) result |= 2;
        if (data[rowIndex].Read()[7 - x]) result |= 1;

        return result;
    }

    public Byte Read(Address address) => this[address].Read();

    public void Write(Address address, Byte value) => this[address].Write(value);

}
