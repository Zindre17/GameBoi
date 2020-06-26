struct Byte
{
    private readonly byte value;
    public Byte(byte value) { this.value = value; }

    public static implicit operator byte(Byte b) => b.value;
    public static implicit operator Byte(byte value) => new Byte(value);

    public static Byte operator +(Byte a, Byte b) => new Byte((byte)(a.value + b.value));
    public static Byte operator -(Byte a, Byte b) => new Byte((byte)(a.value - b.value));
    public static Byte operator *(Byte a, Byte b) => new Byte((byte)(a.value * b.value));
    public static Byte operator /(Byte a, Byte b) => new Byte((byte)(a.value / b.value));
    public static Byte operator |(Byte a, Byte b) => new Byte((byte)(a.value | b.value));
    public static Byte operator &(Byte a, Byte b) => new Byte((byte)(a.value & b.value));
    public static Byte operator %(Byte a, Byte b) => new Byte((byte)(a.value % b.value));
    public static Byte operator ^(Byte a, Byte b) => new Byte((byte)(a.value ^ b.value));
    public static Byte operator ~(Byte a) => new Byte((byte)(~a.value));
    public static Byte operator <<(Byte a, int b) => new Byte((byte)(a.value << b));
    public static Byte operator >>(Byte a, int b) => new Byte((byte)(a.value >> b));

}