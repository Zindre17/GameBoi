public static class ByteOperations
{
    public static bool TestMask(byte mask, byte source)
    {
        return (mask & source) == mask;
    }

    public static bool TestBit(int bit, byte source)
    {
        return TestMask((byte)(1 << bit), source);
    }

    public static byte Invert(byte b)
    {
        return (byte)(b ^ 0xFF);
    }

    public static byte GetHighNibble(byte source)
    {
        return (byte)(source & 0xF0);
    }

    public static byte GetLowNibble(byte source)
    {
        return (byte)(source & 0x0F);
    }

    public static byte SetBit(int bit, byte target)
    {
        return (byte)(target | (1 << bit));
    }

    public static byte ResetBit(int bit, byte target)
    {
        return (byte)(target & Invert((byte)(1 << bit)));
    }

    public static ushort ConcatBytes(byte h, byte l)
    {
        return (ushort)((h << 8) | l);
    }

    public static byte GetLowByte(ushort doubleWord)
    {
        return (byte)doubleWord;
    }

    public static byte GetHighByte(ushort doubleWord)
    {
        return (byte)(doubleWord >> 8);
    }

    public static byte SwapNibbles(byte source)
    {
        int highNibble = GetHighNibble(source);
        int lowNibble = GetLowNibble(source);
        return (byte)((lowNibble << 4) | (highNibble >> 4));
    }

    public static Address Add16(ushort a, ushort b, out bool C, out bool H)
    {
        int result = a + b;
        C = result > 0xFFFF;
        Address alow12 = a & 0x0FFF;
        Address blow12 = b & 0x0FFF;
        H = alow12 + blow12 > 0x0FFF;
        return result;
    }

    public static Byte Add8(byte a, byte b, out bool C, out bool H, bool withCarry = false)
    {
        int result = a + b;
        Byte low4a = a & 0x0F;
        Byte low4b = b & 0x0F;
        int lowRes = low4a + low4b;
        if (withCarry)
        {
            lowRes++;
            result++;
        }
        H = lowRes > 0x0F;
        C = result > 0xFF;
        return result;
    }

    public static Address Sub16(ushort a, ushort b, out bool C, out bool H)
    {
        int result = a - b;
        C = result < 0;
        Address alow12 = a & 0x0FFF;
        Address blow12 = b & 0x0FFF;
        H = alow12 - blow12 < 0;
        return result;
    }

    public static Byte Sub8(ushort a, ushort b, out bool C, out bool H, bool withCarry = false)
    {
        int result = a - b;
        Byte alow4 = a & 0xF;
        Byte blow4 = b & 0xF;
        int lowRes = alow4.Value - blow4.Value;
        if (withCarry)
        {
            result--;
            lowRes--;
        }
        C = result < 0;
        H = lowRes < 0;
        return result;
    }

}