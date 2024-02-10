namespace Gameboi.Tools;

internal readonly record struct Instruction(int Address, int OpCode, IArgument Argument)
{
    public int Length => 1 + Argument.ArgumentType switch
    {
        ArgumentType.None => 0,
        ArgumentType.Byte => 1,
        ArgumentType.SignedByte => 1,
        ArgumentType.Address => 2,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override string ToString() => $"0x{Address:X4}: 0x{OpCode:X2} - {AssemblyConverter.Decompile(OpCode, Argument)}";

    public bool IsRelativeJump() => OpCode is 0x18;
    public bool IsAbsoluteJump() => OpCode is 0xc3 or 0xcf or 0xe9;
    public bool IsReturn() => OpCode is 0xc9 or 0xd9;
    public bool IsCall() => OpCode is 0xc4 or 0xc7 or 0xcc or 0xcd or 0xcf or 0xd4 or 0xd7 or 0xdc or 0xdf or 0xe7 or 0xef or 0xf7 or 0xff;
}
