namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class SpeedSwitch : Register
    {
        public override void Write(Byte value) => data = value & 1;
        public override Byte Read() => data | 0x7E;

        public void SwapSpeed()
        {
            data = 0xFE;
        }
    }
}