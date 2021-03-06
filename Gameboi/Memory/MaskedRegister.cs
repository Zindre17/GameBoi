namespace GB_Emulator.Gameboi.Memory
{
    public class MaskedRegister : Register
    {
        protected Byte mask;

        public MaskedRegister(byte mask = 0, bool isReadOnly = false) : base(mask, isReadOnly) => this.mask = mask;

        public override void Write(Byte value) => base.Write(value | mask);

    }
}