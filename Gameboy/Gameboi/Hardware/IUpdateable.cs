namespace GB_Emulator.Gameboi.Hardware
{
    public interface IUpdatable
    {
        void Update(byte cycles, ulong speed);
    }
}