namespace Gameboi.Tools;

internal readonly record struct Instruction(int Address, int OpCode, IArgument Argument)
{
    public override string ToString() => $"0x{Address:X4}: 0x{OpCode:X2} - {AssemblyConverter.Decompile(OpCode, Argument)}";
}
