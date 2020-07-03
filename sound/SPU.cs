using static ByteOperations;
class SPU : Hardware<MainBus>
{
    private static readonly Address NR10_address = 0xFF10;
    private static readonly Address NR11_address = 0xFF11;
    private static readonly Address NR12_address = 0xFF12;
    private static readonly Address NR13_address = 0xFF13;
    private static readonly Address NR14_address = 0xFF14;
    private static readonly Address NR21_address = 0xFF16;
    private static readonly Address NR22_address = 0xFF17;
    private static readonly Address NR23_address = 0xFF18;
    private static readonly Address NR24_address = 0xFF19;
    private static readonly Address NR30_address = 0xFF1A;
    private static readonly Address NR31_address = 0xFF1B;
    private static readonly Address NR32_address = 0xFF1C;
    private static readonly Address NR33_address = 0xFF1D;
    private static readonly Address NR34_address = 0xFF1E;
    private static readonly Address NR41_address = 0xFF20;
    private static readonly Address NR42_address = 0xFF21;
    private static readonly Address NR43_address = 0xFF22;
    private static readonly Address NR44_address = 0xFF23;
    private static readonly Address NR50_address = 0xFF24;
    private static readonly Address NR51_address = 0xFF25;
    private static readonly Address NR52_address = 0xFF26;


    // gb = 2048 - (131072 / Hz)
    // Hz = 131072 / (2048 - gb)

    // 2 outs => SO1, SO2
    // 1 in => VIn

    // 4 types
    //      quadrangular wave patterns with sweep and envelope functions
    //      Quadrangular wave patterns with envelope functions
    //      Voluntary wave pattrens from wave RAM
    //      White noise with envelope function

    // can mix and match these four sounds to the desired output terminals

    // sound ON flag is reset  and sound output stops when:
    //      sound stopped by length of counter
    //      overflow occurs at the addition mode while sweep is operanting at sound 1

    private Byte NR10;
    private Byte NrOfSweepShift => NR10 & 7;
    private bool Addition => TestBit(3, NR10);
    private Byte SweepTime => NR10 & 0x70;

    //change of frequency(NR13, NR14):
    //   X(t) = X(t-1) +/- X(t-1)/2^n

    private Byte NR11;
    private Byte WavePatternDuty => NR11 & 0xC0;
    private Byte SoundLengthData => NR11 & 0x3F;
    // Sound length = (64 - t1) * (1/256) seconds

    private Byte NR12;
    private Byte InitialVolumeOfEnvelope => NR12 & 0xF0;
    private bool IsUp => TestBit(3, NR12);
    private Byte NrOfEnvelopeSweep => NR12 & 7; // n
    // Length of 1 step = n* (1/64) seconds

    private Byte NR13; // low 8 bits of frequency
    private Byte NR14;
    private bool Initial => TestBit(7, NR14);
    private bool CounterSelect => TestBit(6, NR14);
    /*
    Counter /consecutive Selection               
    0 = Regardless of the length data in NR11 sound can be produced consecutively.               
    1 = Sound is generated during the time period set by the length data in NR11. 
    After this period the sound 1 ON flag (bit 0 of NR52) is reset.
    */

    private Byte NR50;
    private bool VinToSO2 => TestBit(7, NR50);
    private Byte SO2Volume => (NR50 & 0x70) >> 4;
    private bool VinToSO1 => TestBit(3, NR50);
    private Byte SO1Volume => NR50 & 7;

    private Byte NR51;
    private bool Sound4ToSO2 => TestBit(7, NR51);
    private bool Sound3ToSO2 => TestBit(6, NR51);
    private bool Sound2ToSO2 => TestBit(5, NR51);
    private bool Sound1ToSO2 => TestBit(4, NR51);
    private bool Sound4ToSO1 => TestBit(3, NR51);
    private bool Sound3ToSO1 => TestBit(2, NR51);
    private bool Sound2ToSO1 => TestBit(1, NR51);
    private bool Sound1ToSO1 => TestBit(0, NR51);


    private Byte FrequensyHi => NR14 & 7;


    private Byte NR52;
    private bool AllSoundOn => TestBit(7, NR52);

    // all below are readonly -------------------
    private bool Sound4On => TestBit(3, NR52);
    private bool Sound3On => TestBit(2, NR52);
    private bool Sound2On => TestBit(1, NR52);
    private bool Sound1On => TestBit(0, NR52);
    //-------------------------------------------


    public void Tick()
    {
        NR10 = Read(NR10_address);

    }
}