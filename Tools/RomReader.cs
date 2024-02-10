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

    public Instruction ReadInstruction(RomLocation location)
    {
        UpdatePosition(location);
        var locationAddress = file.Position;
        var opCode = file.ReadByte();
        return new Instruction((int)locationAddress, opCode, ReadArgument(opCode));
    }

    private IArgument ReadArgument(int opCode) => AssemblyConverter.GetInstructionArgumentType(opCode) switch
    {
        ArgumentType.None => NoneArgument.Instance,
        ArgumentType.Byte => Argument.Byte(ReadByte()),
        ArgumentType.SignedByte => Argument.SignedByte((sbyte)ReadByte()),
        ArgumentType.Address => Argument.Address(ReadAddress()),
        _ => throw new NotImplementedException()
    };

    private void UpdatePosition(RomLocation location) => file.Position = location.Bank * 0x4000 + location.Address;

    private int ReadAddress() => file.ReadByte() | (file.ReadByte() << 8);
    private int ReadByte() => file.ReadByte();

    public void Dispose()
    {
        file.Dispose();
    }
}
