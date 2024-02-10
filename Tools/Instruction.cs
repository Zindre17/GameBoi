namespace Gameboi.Tools;

internal readonly record struct Instruction(int Address, int OpCode, IArgument Argument);
