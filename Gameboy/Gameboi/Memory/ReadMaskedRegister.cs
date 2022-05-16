namespace GB_Emulator.Gameboi.Memory
{
    public class ReadMaskedRegister : Register
    {
        private readonly Byte readMask;
        public ReadMaskedRegister(Byte readMask, Byte initialValue, bool isReadOnly = false) : base(initialValue, isReadOnly)
        {
            this.readMask = readMask;
        }

        public override Byte Read() => data | readMask;
    }
}