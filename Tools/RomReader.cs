namespace Gameboi.Tools;

internal class RomReader : IDisposable
{
    public RomReader(string romPath)
    {
        if (!File.Exists(romPath))
        {
            throw new FileNotFoundException("The file does not exist", romPath);
        }
        file = File.OpenRead(romPath);
    }

    private readonly FileStream file;

    public int ReadByte(RomLocation location)
    {
        file.Position = location.Bank * 0x4000 + location.Address;
        return file.ReadByte();
    }

    public int ReadAddress(RomLocation location)
    {
        file.Position = location.Bank * 0x4000 + location.Address;
        return file.ReadByte() | (file.ReadByte() << 8);
    }

    public void Dispose()
    {
        file.Dispose();
    }
}
