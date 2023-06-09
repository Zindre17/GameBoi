using Gameboi.Extensions;

namespace Gameboi.Sound;

public readonly struct Sweep
{
    private readonly SystemState state;

    public Sweep(SystemState state) => this.state = state;

    private byte Data => state.NR10;

    public int SweepTime => (Data & 0x70) >> 4;
    public bool IsSubtraction => Data.IsBitSet(3);
    public int SweepShift => Data & 7;

    private bool IsActive => SweepTime != 0 && SweepShift != 0;

    public void UpdateFrequency()
    {
        if (!IsActive)
        {
            return;
        }

        var frequencyData = state.NR13 | ((state.NR14 & 0x07) << 8);
        var divisor = 1 << SweepShift;

        if (IsSubtraction)
        {
            var prev = frequencyData;
            frequencyData -= frequencyData / divisor;
            if (frequencyData < 0)
            {
                frequencyData = prev;
            }

        }
        else
        {
            frequencyData += frequencyData / divisor;
            if (frequencyData >= 0x7FF)
            {
                // Turn off channel 1 on overflow
                state.NR52 = state.NR52.UnsetBit(0);
            }
        }

        frequencyData &= 0x7FF;

        // Update NR13 and NR14 (frequency data)
        state.NR13 = (byte)frequencyData;
        state.NR14 = (byte)((state.NR14 & 0xF8) | (frequencyData >> 8));
    }
}
