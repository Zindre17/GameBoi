namespace Gameboi.Memory
{
    public struct Byte
    {
        private readonly byte value;
        public byte Value => value;

        public Byte(byte value) => this.value = value;

        public static implicit operator byte(Byte b) => b.value;
        public static implicit operator Byte(byte value) => new(value);
        public static implicit operator Byte(int value) => new((byte)value);
        public static implicit operator Byte(ulong value) => new((byte)value);
        public static implicit operator Byte(ushort value) => new((byte)value);

        public static explicit operator Address(Byte b) => new(b.value);
        public static explicit operator sbyte(Byte b) => (sbyte)b.Value;

        public override string ToString() => value.ToString("X2");

        public bool this[int bit] => (value & 1 << bit) != 0;

    }
}
