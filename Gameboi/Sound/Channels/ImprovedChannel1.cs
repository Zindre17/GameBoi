using System;
using Gameboi.Extensions;

namespace Gameboi.Sound.channels;

public class ImprovedChannel1
{
    private readonly SystemState state;

    public ImprovedChannel1(SystemState state) => this.state = state;

    public int FrequencyData => state.NR13 | ((state.NR14 & 0x07) << 8);

    public void Tick()
    {
        if (state.PreviousSoundTicks.IsBitSet(1)
            && !state.SoundTicks.IsBitSet(1))
        {
            var sweep = new SimpleSweep(state);
            state.TicksSinceLastSweep++;
            if (state.TicksSinceLastSweep >= sweep.SweepTime)
            {
                state.TicksSinceLastSweep = 0;
                sweep.UpdateFrequency();
            }
        }

        if (state.PreviousSoundTicks.IsBitSet(2)
            && !state.SoundTicks.IsBitSet(2))
        {
            var envelope = new SimpleEnvelope(state.Channel1Envelope);
            if (envelope.IsActive)
            {
                state.TicksSinceLastChannel1Envelope++;
                if (state.TicksSinceLastChannel1Envelope >= envelope.StepLength)
                {
                    state.TicksSinceLastChannel1Envelope = 0;
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
                    state.Channel1Envelope = (byte)((currentVolume << 4) | (state.Channel1Envelope & 0x0F));
                }
            }
        }

        if (state.Channel1Duration > 0
            && state.PreviousSoundTicks.IsBitSet(0)
            && !state.SoundTicks.IsBitSet(0))
        {
            state.Channel1Duration -= 1;
            if (state.Channel1Duration is 0)
            {
                state.NR52 = (byte)(state.NR52 & 0xFE);
            }
        }
    }

    private int sampleNr = 0;
    public short[] GetNextSamples(int count)
    {
        short[] samples = new short[count];
        var nr52 = new SimpleNr52(state.NR52);
        if (!nr52.IsChannelOn(0))
        {
            return samples;
        }

        var frequency = 0x20000 / (0x800 - FrequencyData);
        var samplesPerPeriod = Math.Max((int)(Statics.WavSettings.SAMPLE_RATE / frequency), 2);

        var waveDuty = new SimpleWaveDuty(state.NR11);
        var lowToHigh = Math.Max(1, (int)(samplesPerPeriod * waveDuty.GetDuty()));

        var envelope = new SimpleEnvelope(state.Channel1Envelope);
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
