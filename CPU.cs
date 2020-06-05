class CPU
{
    #region Registers

    private byte A;
    private byte F; //flag register

    private byte B;
    private byte C;

    private byte D;
    private byte E;

    private byte H;
    private byte L;

    private ushort PC; //progarm counter

    private ushort SP; //stack pointer

    #endregion


    #region Flags

    private static byte zeroBitMask = 0b10000000;
    private static byte subtractBitMask = 0b01000000;
    private static byte halfCarryBitMask = 0b00100000;
    private static byte carryBitMask = 0b00010000;
    public bool ZeroFlag => (F & zeroBitMask) == zeroBitMask;
    public bool SubtractFlag => (F & subtractBitMask) == subtractBitMask;
    public bool HalfCarryFlag => (F & halfCarryBitMask) == halfCarryBitMask;
    public bool CarryFlag => (F & carryBitMask) == carryBitMask;

    #endregion

    // source http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf
}