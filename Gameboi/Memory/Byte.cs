public struct Byte
{
    private readonly byte value;
    public Byte(byte value) { this.value = value; }
    public byte Value => value;

    public static implicit operator byte(Byte b) => b.value;
    public static implicit operator Byte(byte value) => new Byte(value);
    public static implicit operator Byte(int value) => new Byte((byte)value);
    public static explicit operator Address(Byte b) => new Address(b.value);

    public static explicit operator sbyte(Byte b) => (sbyte)b.Value;

    public override string ToString() => value.ToString("X2");

    public static Address operator +(Byte a, Byte b) => a.value + b.value;
    public static Address operator -(Byte a, Byte b) => a.value - b.value;
    public static Address operator *(Byte a, Byte b) => a.value * b.value;
    public static Address operator /(Byte a, Byte b) => a.value / b.value;
    public static Byte operator |(Byte a, Byte b) => a.value | b.value;
    public static Byte operator &(Byte a, Byte b) => a.value & b.value;
    public static Byte operator %(Byte a, Byte b) => a.value % b.value;
    public static Byte operator ^(Byte a, Byte b) => a.value ^ b.value;
    public static Byte operator ~(Byte a) => ~a.value;
    public static int operator <<(Byte a, int b) => a.value << b;
    public static int operator >>(Byte a, int b) => a.value >> b;

}