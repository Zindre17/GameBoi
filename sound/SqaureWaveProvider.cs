using static WavSettings;

class SquareWaveProvider : ISampleProvider
{
    private bool isStopped = true;

    public SquareWaveProvider()
    {
        UpdateSamplingIntermediates();
        UpdateDuration();
    }

    public void Start() => isStopped = false;
    public void Stop() { isStopped = true; volume = 0; }

    private uint frequency = 400;
    public void UpdateFrequency(uint value)
    {
        frequency = value;
        UpdateSamplingIntermediates();
    }

    private double duty = 0.5;
    public void UpdateDuty(double value)
    {
        duty = value;
        UpdateSamplingIntermediates();
    }

    private Address volume = 8;

    public void UpdateVolume(ushort value)
    {
        if (isStopped)
            volume = 0;
        else
            volume = value;
    }


    private int durationInSamples = 0;
    private double duration = 0;
    public void UpdateDuration(double value)
    {
        duration = value;
        UpdateDuration();
        samplesThisDuration = 0;
    }
    private void UpdateDuration() => durationInSamples = (int)(SAMPLE_RATE * duration);

    private int lowToHigh = 0;
    private int samplesPerPeriod = 2;
    private void UpdateSamplingIntermediates()
    {
        samplesPerPeriod = (int)(SAMPLE_RATE / frequency);
        if (samplesPerPeriod == 0)
            samplesPerPeriod = 2;
        lowToHigh = (int)(samplesPerPeriod * duty);
    }

    private bool HasDuration => duration != 0;

    private int sampleNr = 0;
    private int samplePoint = 0;
    private int samplesThisDuration = 0;

    public short GetNextSample()
    {
        if (HasDuration)
        {
            if (samplesThisDuration >= durationInSamples)
            {
                isStopped = true;
                return 0;
            }
        }

        samplePoint %= samplesPerPeriod;

        sampleNr++;
        samplesThisDuration++;
        return (short)(volume * (samplePoint++ > lowToHigh ? 1 : -1));
    }

}