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

    public static bool IsHalfCarryOnAddition(byte source, byte operand)
    {
        int sourceLowNibble = GetLowNibble(source);
        int operandLowNibble = GetLowNibble(operand);
        return (sourceLowNibble + operandLowNibble) > 0x0F;
    }

    public static bool IsCarryOnAddition(int result)
    {
        return result > 0x00FF;
    }

    public static bool IsHalfCarryOnSubtraction(byte source, byte operand)
    {
        int sourceLowNibble = GetLowNibble(source);
        int operandLowNibble = GetLowNibble(operand);
        return sourceLowNibble - operandLowNibble < 0;
    }

    public static bool IsCarryOnSubtraction(int result)
    {
        return result < 0;
    }

    public static byte SwapNibbles(byte source)
    {
        int highNibble = GetHighNibble(source);
        int lowNibble = GetLowNibble(source);
        return (byte)((lowNibble << 4) | (highNibble >> 4));
    }
}