using System;
using Gameboi.Sound.channels;

namespace Gameboi.Sound;

public class OldSpuWithNewState
{
    private readonly ImprovedChannel1 channel1;
    private readonly SystemState state;

    public OldSpuWithNewState(SystemState state)
    {
        this.state = state;
        channel1 = new(state);
    }

    public void Tick()
    {
        if (state.SoundTicks == state.PreviousSoundTicks)
        {
            return;
        }

        channel1.Tick();
        // channel2.Tick();
        // channel3.Tick();
        // channel4.Tick();
        state.PreviousSoundTicks = state.SoundTicks;
    }

    public void GenerateNextFrameOfSamples()
    {
        // 735 * 60 = 44100
        AddNextSampleBatch(735);
    }

    private void AddNextSampleBatch(int sampleCount)
    {
        var nr52 = new SimpleNr52(state.NR52);
        if (!nr52.IsSoundOn)
        {
            return;
        }

        var channel1Samples = channel1.GetNextSamples(sampleCount);

        var samples = new short[sampleCount * 2];

        var nr50 = new SimpleNr50(state.NR50);
        var out1volume = nr50.VolumeOut1;
        var out2volume = nr50.VolumeOut2;

        var nr51 = new SimpleNr51(state.NR51);

        int index = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            //channel1
            var c1Sample = 0;

            if (nr51.Is1Out1)
                c1Sample += channel1Samples[i];
            // if (nr51.Is2Out1)
            //     c1Sample += (short)(channel2Samples[i] / 4);
            // if (nr51.Is3Out1)
            //     c1Sample += (short)(channel3Samples[i] / 4);
            // if (nr51.Is4Out1)
            //     c1Sample += (short)(channel4Samples[i] / 4);


            c1Sample = (int)(c1Sample * out1volume / (7d * 15) * short.MaxValue);

            samples[index++] = (short)c1Sample;

            //channel2
            var c2Sample = 0;

            if (nr51.Is1Out2)
                c2Sample += channel1Samples[i];
            // if (nr51.Is2Out2)
            //     c2Sample += (short)(channel2Samples[i] / 4);
            // if (nr51.Is3Out2)
            //     c2Sample += (short)(channel3Samples[i] / 4);
            // if (nr51.Is4Out2)
            //     c2Sample += (short)(channel4Samples[i] / 4);

            c2Sample = (short)(c2Sample * out2volume / (7d * 15) * short.MaxValue);

            samples[index++] = (short)c2Sample;
        }

        var copyIndex = 0;
        if (state.SampleBufferIndex + samples.Length > state.SampleBuffer.Length)
        {
            var remaining = state.SampleBuffer.Length - state.SampleBufferIndex;
            Array.Copy(samples, copyIndex, state.SampleBuffer, state.SampleBufferIndex, remaining);
            copyIndex = remaining;
            state.SampleBufferIndex = 0;
        }
        Array.Copy(samples, copyIndex, state.SampleBuffer, state.SampleBufferIndex, samples.Length - copyIndex);
        state.SampleBufferIndex += samples.Length - copyIndex;
    }
}

