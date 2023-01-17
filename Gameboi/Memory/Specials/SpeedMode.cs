namespace Gameboi.Memory.Specials
{
    public class SpeedMode : Register
    {

        public const ulong NormalSpeed = 1ul;
        public const ulong DoubleSpeed = 2ul;

        public bool ShouldSwapSpeed => data[0];
        public ulong Mode => data[7] ? DoubleSpeed : NormalSpeed;

        public void Reset()
        {
            data = 0;
        }

        public override void Write(Byte value) => data = value & 1;
        public override Byte Read() => data | 0x7E;

        public void SwapSpeed()
        {
            if (data[7])
                data = 0x7E;
            else
                data = 0xFE;
        }
    }
}
