using System;
using System.Collections.Generic;
using NAudio.Wave;
using static WavSettings;

class SquareWaveProvider : IWaveProvider
{
    private WaveFormat format = new WaveFormat();
    private bool isStopped = true;

    public WaveFormat WaveFormat => format;

    public SquareWaveProvider()
    {
        UpdateSamplingIntermediates();
        UpdateDuration();
    }

    public void Start() => isStopped = false;
    public void Stop() => isStopped = true;

    private List<Action> queuedUpdates = new List<Action>();

    public void ApplyUpdates()
    {
        if (queuedUpdates.Count != 0)
        {
            foreach (var update in queuedUpdates)
                update();
            queuedUpdates.Clear();
        }
    }

    private uint frequency = 400;
    public void UpdateFrequency(uint value)
    {
        // queuedUpdates.Add(() =>
        // {
        frequency = value;
        UpdateSamplingIntermediates();
        // });
    }

    private double duty = 0.5;
    public void UpdateDuty(double value)
    {
        // queuedUpdates.Add(() =>
        // {
        duty = value;
        UpdateSamplingIntermediates();
        // });
    }

    private Address volume = 8;

    public void UpdateVolume(ushort value)
    {
        // queuedUpdates.Add(() =>
        // {
        volume = value & 0xF;
        // });
    }


    private int durationInSamples = 0;
    private double duration = 0;
    public void UpdateDuration(double value)
    {
        // queuedUpdates.Add(() =>
        // {
        duration = value;
        UpdateDuration();
        // });
    }
    private void UpdateDuration() => durationInSamples = (int)(SAMPLE_RATE * duration);

    private int lowToHigh = 0;
    private int samplesPerPeriod = 1;
    private void UpdateSamplingIntermediates()
    {
        samplesPerPeriod = (int)(SAMPLE_RATE / frequency);
        lowToHigh = (int)(samplesPerPeriod * duty);
    }

    private int sampleNr = 0;
    private short GetNextSample()
    {
        if (durationInSamples != 0)
            if (sampleNr >= durationInSamples)
            {
                sampleNr = 0;
                isStopped = true;
                return 0;
            }
        var samplePoint = sampleNr++ % samplesPerPeriod;
        // if (samplePoint == 0)
        //     ApplyUpdates();
        return (short)(volume * (samplePoint > lowToHigh ? -1 : 1));
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (isStopped) return 0;

        int bytesRead = 0;
        while (bytesRead < count)
        {
            short sample = GetNextSample();

            if (isStopped) break;

            Byte high = sample >> 8;
            Byte low = sample;

            //channel1
            buffer[bytesRead++] = high;
            buffer[bytesRead++] = low;
            //channel2
            buffer[bytesRead++] = high;
            buffer[bytesRead++] = low;
        }

        return bytesRead;
    }
}