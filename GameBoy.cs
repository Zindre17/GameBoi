using System;

class GameBoy
{
    private CPU cpu;
    private Screen screen;
    private Controller controller;
    private Cartridge game;
    private MainBus bus;
    private Speaker speaker;

    private bool isPowered = false;
    public GameBoy()
    {
        // Create hardware
        bus = new MainBus();
        cpu = new CPU();
        screen = new Screen();
        controller = new Controller();

        //Connect it all to the bus
        cpu.Connect(bus);
        controller.Connect(bus);
    }


    private void InterruptProcedure()
    {
        // Interrupt => IF flag set
        // If IME flag && IE flag: 
        //  1: Reset IME flag
        //  2: Push PC to stack
        //  3: Jump to starting address of  interrupt
    }

    private void Play()
    {
        while (isPowered)
        {
            //check for poweroff
            cpu.Tick();
        }
    }

    private void Freeze()
    {
        while (isPowered)
        {
            //Check for poweroff
        }
    }
    public void TurnOn()
    {
        isPowered = true;
        //start internal program at 0x0

        //if check ok => run program at 0x100 on cartridge
        //with following vaules in registers : 
        // AF=0x01-GB/SGB, 0xFF-GBP, 0x11-GBC   
        // F =0xB0
        // C=$0013   
        // DE=$00D8   
        // HL=$014D   
        // Stack Pointer=$FFFE
        // [$FF05] = $00   ; TIMA   
        // [$FF06] = $00   ; TMA   
        // [$FF07] = $00   ; TAC   
        // [$FF10] = $80   ; NR10   
        // [$FF11] = $BF   ; NR11   
        // [$FF12] = $F3   ; NR12   
        // [$FF14] = $BF   ; NR14   
        // [$FF16] = $3F   ; NR21   
        // [$FF17] = $00   ; NR22   
        // [$FF19] = $BF   ; NR24   
        // [$FF1A] = $7F   ; NR30   
        // [$FF1B] = $FF   ; NR31   
        // [$FF1C] = $9F   ; NR32   
        // [$FF1E] = $BF   ; NR33   
        // [$FF20] = $FF   ; NR41   
        // [$FF21] = $00   ; NR42   
        // [$FF22] = $00   ; NR43   
        // [$FF23] = $BF   ; NR30   
        // [$FF24] = $77   ; NR50   
        // [$FF25] = $F3   ; NR51   
        // [$FF26] = $F1-GB, $F0-SGB ; NR52   
        // [$FF40] = $91   ; LCDC   
        // [$FF42] = $00   ; SCY   
        // [$FF43] = $00   ; SCX   
        // [$FF45] = $00   ; LYC   
        // [$FF47] = $FC   ; BGP   
        // [$FF48] = $FF   ; OBP0   
        // [$FF49] = $FF   ; OBP1   
        // [$FF4A] = $00   ; WY   
        // [$FF4B] = $00   ; WX   
        // [$FFFF] = $00   ; IE
        if (true/* replace with cartridge header check passing*/)
            Play();
        else
            Freeze();
    }

    public void TurnOff() { }
}