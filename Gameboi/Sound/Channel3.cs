using System;
using Gameboi.Extensions;

namespace Gameboi.Sound;

public class Channel3
{
    private readonly SystemState state;

    private int FrequencyData => state.NR33 | ((state.NR34 & 0x07) << 8);

    public Channel3(SystemState state)
    {
        this.state = state;
    }

    public void Tick()
    {
        if (state.Channel3Duration > 0
            && state.PreviousSoundTicks.IsBitSet(0)
            && !state.SoundTicks.IsBitSet(0))
        {
            state.Channel3Duration -= 1;
            if (state.Channel3Duration is 0)
            {
                state.NR52 = state.NR52.UnsetBit(2);
            }
        }
    }

    private int GetVolumeShift()
    {
        return ((state.NR32 & 0b0110_0000) >> 5) switch
        {
            0 => 8,
            1 => 0,
            2 => 1,
            3 => 2,
            _ => throw new Exception("Invalid volume shift")
        };
    }

    public short[] GetNextSamples(int sampleCount)
    {
        var samples = new short[sampleCount];
        var nr52 = new Nr52(state.NR52);
        if (!nr52.IsChannelOn(2) || !state.NR30.IsBitSet(7))
        {
            return samples;
        }

        // NOTE: half the frequency of channel 1 and 2
        var frequency = 0x10000 / (double)(0x800 - FrequencyData);
        var frequencyPerWaveSample = frequency * 32d;
        var samplesPerWaveRamSample = Statics.WavSettings.SAMPLE_RATE / frequencyPerWaveSample;

        var volumeShift = GetVolumeShift();

        for (int i = 0; i < sampleCount; i++)
        {
            var index = (int)(state.Channel3SampleNr++ / samplesPerWaveRamSample);
            index %= 32;
            var pair = state.IoPorts[0x30 + (index / 2)];
            var sample = (index % 2) is 0
                ? (pair >> 4) & 0xf
                : pair & 0xf;
            var volumeAdjustedSample = (short)(sample >> volumeShift);
            samples[i] = volumeAdjustedSample;
        }
        return samples;
    }
}
