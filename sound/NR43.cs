public class NR43 : Register
{
    public bool GetStepsSelector()
    {
        return data[3];
    }

    private double GetDivingRatio()
    {
        Byte value = data & 7;
        long f = 0x400000;
        switch (value)
        {
            case 0: return f * (1 / 8f) * 2;
            case 1: return f * (1 / 8f) * 1;
            case 2: return f * (1 / 8f) * (1f / 2);
            case 3: return f * (1 / 8f) * (1f / 3);
            case 4: return f * (1 / 8f) * (1f / 4);
            case 5: return f * (1 / 8f) * (1f / 5);
            case 6: return f * (1 / 8f) * (1f / 6);
            case 7: return f * (1 / 8f) * (1f / 7);

            default: throw new System.Exception();
        }
    }

    public double GetShiftFrequency()
    {
        Byte value = (data & 0xE0) >> 5;
        if (value == 0xE || value == 0xF) throw new System.Exception();
        double divRatio = GetDivingRatio();
        return divRatio * (1d / (1 << (value + 1)));
    }
}