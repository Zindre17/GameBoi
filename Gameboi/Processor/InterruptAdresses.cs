namespace Gameboi.Processor;

public static class InterruptAddresses
{
    public const ushort VblankVector = 0x0040;
    public const ushort LcdStatVector = 0x0048;
    public const ushort TimerVector = 0x0050;
    public const ushort SerialVector = 0x0058;
    public const ushort JoypadVector = 0x0060;
}
