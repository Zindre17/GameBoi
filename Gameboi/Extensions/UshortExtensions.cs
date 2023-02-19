namespace Gameboi.Extensions;

public static class UshortExtensions
{
    public static byte GetLowByte(this ushort value)
    {
        return (byte)(value & 0xff);
    }

    public static byte GetHighByte(this ushort value)
    {
        return (byte)(value >> 8);
    }

}
