using System;
using Gameboi.Extensions;

namespace Gameboi.Sound.channels;

public class ImprovedChannel4
{
    private readonly SystemState state;

    public ImprovedChannel4(SystemState state)
    {
        this.state = state;
    }

    public void Tick()
    {
        if (state.PreviousSoundTicks.IsBitSet(2)
            && !state.SoundTicks.IsBitSet(2))
        {
            var envelope = new SimpleEnvelope(state.Channel4Envelope);
            if (envelope.IsActive)
            {
                state.TicksSinceLastChannel4Envelope++;
                if (state.TicksSinceLastChannel4Envelope >= envelope.StepLength)
                {
                    state.TicksSinceLastChannel4Envelope = 0;
                    var currentVolume = envelope.InitialVolume;
                    if (envelope.IsIncrease)
                    {
                        currentVolume += 1;
                        currentVolume = Math.Min(currentVolume, 0x0F);
                    }
                    else
                    {
                        currentVolume -= 1;
                        currentVolume = Math.Max(currentVolume, 0);
                    }
                    state.Channel4Envelope = (byte)((currentVolume << 4) | (state.Channel4Envelope & 0x0F));
                }
            }
        }

        if (state.Channel4Duration > 0
            && state.PreviousSoundTicks.IsBitSet(0)
            && !state.SoundTicks.IsBitSet(0))
        {
            state.Channel4Duration -= 1;
            if (state.Channel4Duration is 0)
            {
                state.NR52 = state.NR52.UnsetBit(3);
            }
        }
    }

    public short[] GetNextSamples(int sampleCount)
    {
        var samples = new short[sampleCount];

        var nr52 = new SimpleNr52(state.NR52);
        if (!nr52.IsChannelOn(3))
        {
            return samples;
        }
        var nr43 = new SimpleNr43(state.NR43);
        var divider = nr43.ClockDivider;
        if ((int)divider is 0)
        {
            divider = 0.5d;
        }

        var frequency = 0x40_000 / (divider * (1 << nr43.ClockShift));
        var lfsrPerSample = frequency / Statics.WavSettings.SAMPLE_RATE;

        var volume = new SimpleEnvelope(state.Channel4Envelope).InitialVolume;
        var remainingShifts = lfsrPerSample;
        var sample = volume * (state.Lfsr & 1);
        for (var i = 0; i < sampleCount; i++)
        {
            while (remainingShifts > 1)
            {
                Shift(nr43.IsWidth7);
                remainingShifts -= 1;
                sample = volume * (state.Lfsr & 1);
            }
            samples[i] = (short)(sample);
            remainingShifts += lfsrPerSample;
        }
        return samples;
    }

    private void Shift(bool isWidth7)
    {
        var currentLfsr = state.Lfsr;
        var bit0 = currentLfsr & 1;
        var bit1 = (currentLfsr >> 1) & 1;
        if (bit0 == bit1)
        {
            currentLfsr |= 1 << 15;
            if (isWidth7)
            {
                currentLfsr |= 1 << 7;
            }
        }
        else
        {
            if (isWidth7)
            {
                currentLfsr &= ~(1 << 7);
            }
        }
        state.Lfsr = currentLfsr >> 1;
    }
}
