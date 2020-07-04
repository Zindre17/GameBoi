public struct Byte
{
    private readonly byte value;
    public byte Value => value;

    public Byte(byte value) => this.value = value;

    public static implicit operator byte(Byte b) => b.value;
    public static implicit operator Byte(byte value) => new Byte(value);
    public static implicit operator Byte(int value) => new Byte((byte)value);
    public static implicit operator Byte(ulong value) => new Byte((byte)value);

    public static explicit operator Address(Byte b) => new Address(b.value);
    public static explicit operator sbyte(Byte b) => (sbyte)b.Value;

    public override string ToString() => value.ToString("X2");

}