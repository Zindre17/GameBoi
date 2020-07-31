
abstract class SoundChannel : Hardware
{

    public abstract void Tick(Byte cpuCycles);

    public abstract override void Connect(Bus bus);

}