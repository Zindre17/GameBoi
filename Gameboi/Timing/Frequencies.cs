namespace Gameboi.Timing;

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
