namespace GB_Emulator.Memory
{
    public interface IMemory
    {
        Byte Read();
        void Write(Byte value);
    }
}
