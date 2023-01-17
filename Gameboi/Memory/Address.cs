namespace Gameboi.Memory
{
    public struct Address
    {
        private readonly ushort value;

        public Address(ushort value) { this.value = value; }

        public static implicit operator ushort(Address a) => a.value;
        public static implicit operator Address(ushort v) => new(v);
        public static implicit operator Address(int v) => new((ushort)v);
        public static implicit operator Byte(Address a) => (byte)a.value;
        public static implicit operator Address(double a) => new((ushort)a);

        public override string ToString() => value.ToString("X4");

    }
}
