using System;
using Gameboi.Extensions;

namespace Gameboi.Sound.channels;

public class ImprovedChannel3
{
    private readonly SystemState state;

    private int FrequencyData => state.NR33 | ((state.NR34 & 0x07) << 8);

    public ImprovedChannel3(SystemState state)
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

    private byte GetWaveRamSample()
    {
        var sampleNr = state.Channel3SampleNr;
        state.Channel3SamplesForCurrentWaveSample += 1;
        var index = IoIndices.WAVE_RAM_START_index + (sampleNr / 2);
        var waveRamSample = state.IoPorts[index];
        return (byte)((sampleNr % 2) is 0
            ? (waveRamSample >> 0) & 0x0F
            : (waveRamSample >> 4) & 0x0F);
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
        var nr52 = new SimpleNr52(state.NR52);
        if (!nr52.IsChannelOn(2) || !state.NR30.IsBitSet(7))
        {
            return samples;
        }

        // NOTE: half the frequency of channel 1 and 2
        var frequency = 0x10000 / (0x800 - FrequencyData);
        var frequencyPerWaveSample = frequency * 32d;
        var samplesPerWaveRamSample = (int)(Statics.WavSettings.SAMPLE_RATE / frequencyPerWaveSample);

        var volumeShift = GetVolumeShift();

        for (int i = 0; i < sampleCount; i++)
        {
            var waveRamSample = GetWaveRamSample();
            samples[i] = (short)(waveRamSample >> volumeShift);

            if (state.Channel3SamplesForCurrentWaveSample >= samplesPerWaveRamSample)
            {
                state.Channel3SamplesForCurrentWaveSample = 0;
                state.Channel3SampleNr++;
                state.Channel3SampleNr %= 32;
            }
        }
        return samples;
    }
}
