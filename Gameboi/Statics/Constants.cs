namespace Gameboi.Statics;

public static class ScreenTimings
{
    public const uint clocksPerDraw = 70224;
    public const ushort vblankClocks = 4560;
    public const byte mode2Clocks = 80;
    public const byte mode3Clocks = 172;
    public const ushort hblankClocks = 204;
}

public static class InterruptAddresses
{
    public const ushort VblankVector = 0x0040;
    public const ushort LcdStatVector = 0x0048;
    public const ushort TimerVector = 0x0050;
    public const ushort SerialVector = 0x0058;
    public const ushort JoypadVector = 0x0060;
}

public static class Frequencies
{
    // TAC speeds
    // bit 2 : 0 = Stop, 1 = Start
    // bit 1 - 0: 
    //      00 = 4096Hz = 0x1000Hz,
    //      01 = 262144Hz = 0x40000Hz, 
    //      10 = 65536Hz = 0x10000Hz, 
    //      11 = 16384Hz = 0x4000Hz  
    public static readonly uint[] timerSpeeds = new uint[4]{
        0x1000,
        0x40000,
        0x10000,
        0x4000
    };

    public const uint cpuSpeed = 0x400000;

    public static readonly uint[] ticksPerIncrementPerTimerSpeed = new uint[4]{
        cpuSpeed / timerSpeeds[0],
        cpuSpeed / timerSpeeds[1],
        cpuSpeed / timerSpeeds[2],
        cpuSpeed / timerSpeeds[3]
    };

    // DIV is incremented at 16384Hz = 0x4000Hz
    public static readonly uint ticksPerDivIncrement = ticksPerIncrementPerTimerSpeed[3];
}

public static class WavSettings
{
    public const string FILE_TYPE_ID = "RIFF";
    public const uint FILE_LENGTH = 4 + 8 + FORMAT_SIZE + 8 + DATA_SIZE;
    public const string MEDIA_TYPE_ID = "WAVE";

    public const string FORMAT_ID = "fmt ";
    public const uint FORMAT_SIZE = 16;
    public const ushort FORMAT_TAG = 1;
    public const ushort CHANNELS = 1;
    public const uint SAMPLE_RATE = 44100;
    public const uint AVG_BYTES_PER_SEC = (uint)(SAMPLE_RATE * 0.05 * BLOCK_ALIGN);
    public const ushort BLOCK_ALIGN = CHANNELS * (BITS_PER_SAMPLE / 8);
    public const ushort BITS_PER_SAMPLE = 16;

    public const string DATA_ID = "data";
    public const uint DATA_SIZE = SAMPLE_RATE * BLOCK_ALIGN; // 100ms

    public const long DATA_SAMPLE_START_INDEX = FILE_LENGTH + 8 - DATA_SIZE;
}
