namespace GB_Emulator.Gameboi.Memory
{
    public interface IMemory
    {
        Byte Read();
        void Write(Byte value);
    }
}