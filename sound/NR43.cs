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
        return (byte)value switch
        {
            0 => f * (1 / 8f) * 2,
            1 => f * (1 / 8f) * 1,
            2 => f * (1 / 8f) * (1f / 2),
            3 => f * (1 / 8f) * (1f / 3),
            4 => f * (1 / 8f) * (1f / 4),
            5 => f * (1 / 8f) * (1f / 5),
            6 => f * (1 / 8f) * (1f / 6),
            7 => f * (1 / 8f) * (1f / 7),
            _ => throw new System.Exception(),
        };
    }

    public double GetShiftFrequency()
    {
        Byte value = (data & 0xE0) >> 5;
        if (value == 0xE || value == 0xF) throw new System.Exception();
        double divRatio = GetDivingRatio();
        return divRatio * (1d / (1 << (value + 1)));
    }
}