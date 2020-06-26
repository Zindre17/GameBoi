struct Address
{
    private readonly ushort value;
    public Address(ushort value) { this.value = value; }

    public static implicit operator ushort(Address a) => a.value;
    public static implicit operator Address(ushort v) => new Address(v);
    public static implicit operator Address(int v) => new Address((ushort)v);

    public static Address operator +(Address a, Address b) => new Address((ushort)(a.value + b.value));
    public static Address operator -(Address a, Address b) => new Address((ushort)(a.value - b.value));
    public static Address operator /(Address a, Address b) => new Address((ushort)(a.value / b.value));
    public static Address operator *(Address a, Address b) => new Address((ushort)(a.value * b.value));
    public static Address operator |(Address a, Address b) => new Address((ushort)(a.value | b.value));
    public static Address operator &(Address a, Address b) => new Address((ushort)(a.value & b.value));
    public static Address operator ^(Address a, Address b) => new Address((ushort)(a.value ^ b.value));
    public static Address operator %(Address a, Address b) => new Address((ushort)(a.value % b.value));
    public static Address operator ~(Address a) => new Address((ushort)(~a.value));
    public static Address operator <<(Address a, int b) => new Address((ushort)(a.value << b));
    public static Address operator >>(Address a, int b) => new Address((ushort)(a.value >> b));
}