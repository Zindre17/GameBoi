namespace Gameboi.Tools;

internal enum ArgumentType
{
    Address,
    Byte,
    SignedByte,
    None
}

internal interface IArgument
{
    ArgumentType ArgumentType { get; }
    int Value { get; }
}

internal record Argument(ArgumentType ArgumentType, int Value) : IArgument
{
    public static Argument Byte(int value) => new(ArgumentType.Byte, value);
    public static Argument SignedByte(sbyte value) => new(ArgumentType.SignedByte, value);
    public static Argument Address(int value) => new(ArgumentType.Address, value);
}

internal record NoneArgument : IArgument
{
    private NoneArgument() { }
    public static NoneArgument Instance { get; } = new();

    public int Value => throw new InvalidOperationException($"{nameof(NoneArgument)} has no {nameof(Value)}");
    public ArgumentType ArgumentType => ArgumentType.None;
}
