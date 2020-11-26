using System;
using static WavSettings;

public class Envelope : Register
{

    public void Initialize()
    {
        initialVolume = (data & 0xF0) >> 4;
        isIncrease = data[3];
        Byte stepLengthData = data & 7;

        if (stepLengthData == 0 || (isIncrease && initialVolume == 0xF) || (!isIncrease && initialVolume == 0))
            isActive = false;
        else
        {
            var frequency = 64d / stepLengthData;
            samplesPerStep = (int)(SAMPLE_RATE / frequency);
            isActive = true;
        }
    }

    private Byte initialVolume;
    private int samplesPerStep;
    private bool isActive;
    private bool isIncrease;

    public Address GetVolume(int samplesThisDuration)
    {
        Byte currentVolume = initialVolume;
        if (isActive)
        {
            int step = samplesThisDuration / samplesPerStep;
            if (isIncrease)
                currentVolume = Math.Min(0x0F, initialVolume + step);
            else
                currentVolume = Math.Max(0, initialVolume - step);
        }
        return ScaleVolume(currentVolume);
    }

    private Address ScaleVolume(byte volume) => 5 * volume;
}