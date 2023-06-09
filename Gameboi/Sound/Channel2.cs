using System;
using Gameboi.Extensions;

namespace Gameboi.Sound;

public class Channel2
{
    private readonly SystemState state;

    public Channel2(SystemState state) => this.state = state;

    public int FrequencyData => state.NR23 | ((state.NR24 & 0x07) << 8);

    public void Tick()
    {
        if (state.PreviousSoundTicks.IsBitSet(2)
            && !state.SoundTicks.IsBitSet(2))
        {
            var envelope = new Envelope(state.Channel2Envelope);
            if (envelope.IsActive)
            {
                state.TicksSinceLastChannel2Envelope++;
                if (state.TicksSinceLastChannel2Envelope >= envelope.StepLength)
                {
                    state.TicksSinceLastChannel2Envelope = 0;
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
                    state.Channel2Envelope = (byte)((currentVolume << 4) | (state.Channel2Envelope & 0x0F));
                }
            }
        }

        if (state.Channel2Duration > 0
            && state.PreviousSoundTicks.IsBitSet(0)
            && !state.SoundTicks.IsBitSet(0))
        {
            state.Channel2Duration -= 1;
            if (state.Channel2Duration is 0)
            {
                state.NR52 = (byte)(state.NR52 & 0b1111_1101);
            }
        }
    }

    private int sampleNr = 0;
    public short[] GetNextSamples(int count)
    {
        short[] samples = new short[count];
        var nr52 = new Nr52(state.NR52);
        if (!nr52.IsChannelOn(1))
        {
            return samples;
        }

        var frequency = 0x20000 / (0x800 - FrequencyData);
        var samplesPerPeriod = Math.Max((int)(Statics.WavSettings.SAMPLE_RATE / frequency), 2);

        var waveDuty = new WaveDuty(state.NR21);
        var lowToHigh = Math.Max(1, (int)(samplesPerPeriod * waveDuty.GetDuty()));

        var envelope = new Envelope(state.Channel2Envelope);
        var volume = envelope.InitialVolume;

        sampleNr %= samplesPerPeriod;
        for (int i = 0; i < count; i++)
        {
            samples[i] = (short)((sampleNr > lowToHigh ? 1 : 0) * volume);
            sampleNr++;
            sampleNr %= samplesPerPeriod;
        }
        return samples;
    }
}
