using System;
using static TileDataConstants;

public class Tile : IMemoryRange
{
    private readonly IMemory[] data = new IMemory[bytesPerTile];

    public Address Size => bytesPerTile;

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

    public Byte Read(Address address, bool isCpu = false) => data[address].Read();

    public void Write(Address address, Byte value, bool isCpu = false) => data[address].Write(value);

    public void Set(Address address, IMemory replacement) => data[address] = replacement;

}
