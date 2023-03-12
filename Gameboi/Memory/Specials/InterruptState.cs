using Gameboi.Extensions;

namespace Gameboi.Memory.Specials;

public enum InterruptVector : ushort
{
    VerticalBlank = 0x40,
    LcdStatus = 0x48,
    Timer = 0x50,
    Serial = 0x58,
    Joypad = 0x60
}

public readonly struct InterruptState
{
    private readonly byte value;

    public InterruptState(byte value) => this.value = value;
    public InterruptState(int value) => this.value = (byte)value;

    public bool IsVerticalBlankSet => value.IsBitSet(0);
    public bool IsLcdStatusSet => value.IsBitSet(1);
    public bool IsTimerSet => value.IsBitSet(2);
    public bool IsSerialPortSet => value.IsBitSet(3);
    public bool IsJoypadSet => value.IsBitSet(4);

    public bool Any => (value & 0x1f) is not 0;
    public bool HasNone => !Any;
}
