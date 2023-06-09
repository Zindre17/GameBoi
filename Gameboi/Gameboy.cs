using Gameboi.Hardware;

namespace Gameboi;

public class Gameboy
{
    private readonly CPU cpu = new();
    private readonly Bus bus = new();

    public Gameboy()
    {
        bus.Connect(cpu);
    }

    public void ChangeSpeed(bool faster)
    {
        cpu.ChangeSpeed(faster);
    }

    public void PlayForOneFrame()
    {
        cpu.Loop(0);
    }
}

