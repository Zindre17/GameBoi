public class WaveRam : IMemoryRange
{
    public WaveRam()
    {
        for (int i = 0; i < Size; i++)
            ram[i] = new WaveRegister();
    }

    public byte[] GetSamples()
    {
        byte[] samples = new byte[Size * 2];
        int index = 0;
        foreach (var samplePair in ram)
        {
            samples[index++] = samplePair.First;
            samples[index++] = samplePair.Second;
        }
        return samples;
    }

    private WaveRegister[] ram = new WaveRegister[0x10];

    public Address Size => ram.Length;

    public Byte Read(Address address, bool isCpu = false) => ram[address].Read();

    public void Set(Address address, IMemory replacement) => throw new System.NotImplementedException();

    public void Write(Address address, Byte value, bool isCpu = false) => ram[address].Write(value);
}
