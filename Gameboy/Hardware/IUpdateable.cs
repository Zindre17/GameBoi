namespace GB_Emulator.Hardware
{
    public interface IUpdatable
    {
        void Update(uint cycles, ulong speed);
    }
}
