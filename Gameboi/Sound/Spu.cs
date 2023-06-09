using System;

namespace Gameboi.Sound;

public class Spu
{
    private readonly Channel1 channel1;
    private readonly Channel2 channel2;
    private readonly Channel3 channel3;
    private readonly Channel4 channel4;
    private readonly SystemState state;

    public Spu(SystemState state)
    {
        this.state = state;
        channel1 = new(state);
        channel2 = new(state);
        channel3 = new(state);
        channel4 = new(state);
    }

    public void Tick()
    {
        if (state.SoundTicks == state.PreviousSoundTicks)
        {
            return;
        }

        channel1.Tick();
        channel2.Tick();
        channel3.Tick();
        channel4.Tick();
        state.PreviousSoundTicks = state.SoundTicks;
    }

    public void GenerateNextFrameOfSamples()
    {
        // 735 * 60 = 44100
        AddNextSampleBatch(735);
    }

    private const int analogConversionFactor = (short.MaxValue - short.MinValue - 1) / (4 * 0xf);

    private void AddNextSampleBatch(int sampleCount)
    {
        var nr52 = new Nr52(state.NR52);
        if (!nr52.IsSoundOn)
        {
            return;
        }

        var channel1Samples = channel1.GetNextSamples(sampleCount);
        var channel2Samples = channel2.GetNextSamples(sampleCount);
        var channel3Samples = channel3.GetNextSamples(sampleCount);
        var channel4Samples = channel4.GetNextSamples(sampleCount);

        var samples = new short[sampleCount * 2];

        var nr50 = new Nr50(state.NR50);
        var out1volume = nr50.VolumeOut1 / 7d;
        var out2volume = nr50.VolumeOut2 / 7d;

        var nr51 = new Nr51(state.NR51);

        int index = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            //channel1
            var c1Sample = 0;

            if (nr51.Is1Out1)
                c1Sample += channel1Samples[i];
            if (nr51.Is2Out1)
                c1Sample += channel2Samples[i];
            if (nr51.Is3Out1)
                c1Sample += channel3Samples[i];
            if (nr51.Is4Out1)
                c1Sample += channel4Samples[i];

            var analogSampleValue = short.MaxValue - (c1Sample * analogConversionFactor);

            samples[index++] = (short)(analogSampleValue * out1volume);

            //channel2
            var c2Sample = 0;

            if (nr51.Is1Out2)
                c2Sample += channel1Samples[i];
            if (nr51.Is2Out2)
                c2Sample += channel2Samples[i];
            if (nr51.Is3Out2)
                c2Sample += channel3Samples[i];
            if (nr51.Is4Out2)
                c2Sample += channel4Samples[i];

            var analogSampleValue2 = short.MaxValue - (c2Sample * analogConversionFactor);

            samples[index++] = (short)(analogSampleValue2 * out2volume);
        }

        var copyIndex = 0;
        if (state.SampleBufferIndex + samples.Length > state.CurrentSampleBuffer.Length)
        {
            var remaining = state.CurrentSampleBuffer.Length - state.SampleBufferIndex;
            Array.Copy(samples, copyIndex, state.CurrentSampleBuffer, state.SampleBufferIndex, remaining);
            copyIndex = remaining;
            state.SampleBufferIndex = 0;

            var bufferCopy = new short[state.CurrentSampleBuffer.Length];
            Array.Copy(state.CurrentSampleBuffer, bufferCopy, state.CurrentSampleBuffer.Length);
            state.SampleBufferQueue.Enqueue(bufferCopy);
        }
        Array.Copy(samples, copyIndex, state.CurrentSampleBuffer, state.SampleBufferIndex, samples.Length - copyIndex);
        state.SampleBufferIndex += samples.Length - copyIndex;
    }
}

