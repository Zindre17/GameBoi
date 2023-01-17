using Gameboi.Memory;

namespace Gameboi.Sound;

public class FrequencyHigh : ModeRegister
{
    public FrequencyHigh() : base(0x38) { }

    public Byte HighBits => data & 7;

}

