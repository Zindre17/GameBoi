using Gameboi.Memory;

namespace Gameboi.Sound;

public class WaveRam : IMemoryRange
{
    public WaveRam()
    {
        for (int i = 0; i < Size; i++)
            ram[i] = new WaveRegister();
    }

    public byte GetSample(int index)
    {
        index %= Size * 2;
        var pair = ram[index / 2];
        if ((index % 2) == 0)
        {
            return pair.First;
        }
        return pair.Second;
    }

    private readonly WaveRegister[] ram = new WaveRegister[0x10];

    public Address Size => ram.Length;

    public Byte Read(Address address, bool isCpu = false) => ram[address].Read();

    public void Set(Address address, IMemory replacement) => throw new System.NotImplementedException();

    public void Write(Address address, Byte value, bool isCpu = false) => ram[address].Write(value);
}

