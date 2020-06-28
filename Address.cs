public struct Address
{
    private readonly ushort value;
    public Address(ushort value) { this.value = value; }

    public static implicit operator ushort(Address a) => a.value;
    public static implicit operator Address(ushort v) => new Address(v);
    public static implicit operator Address(int v) => new Address((ushort)v);
    public static implicit operator Byte(Address a) => a.value;

    public static int operator +(Address a, Address b) => a.value + b.value;
    public static int operator -(Address a, Address b) => a.value - b.value;
    public static int operator /(Address a, Address b) => a.value / b.value;
    public static int operator *(Address a, Address b) => a.value * b.value;
    public static Address operator |(Address a, Address b) => a.value | b.value;
    public static Address operator &(Address a, Address b) => a.value & b.value;
    public static Address operator ^(Address a, Address b) => a.value ^ b.value;
    public static Address operator %(Address a, Address b) => a.value % b.value;
    public static Address operator ~(Address a) => ~a.value;
    public static int operator <<(Address a, int b) => a.value << b;
    public static int operator >>(Address a, int b) => a.value >> b;
}