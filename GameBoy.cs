using System;

class GameBoy
{
    private CPU cpu;
    private Timer timer;
    public LCD lcd;
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
        timer = new Timer();
        lcd = new LCD();
        controller = new Controller();
        // game = new Cartridge("roms/01-special.gb", true);
        // game = new Cartridge("roms/02-interrupts.gb", true);
        // game = new Cartridge("roms/03-op sp,hl.gb", true);
        // game = new Cartridge("roms/04-op r,imm.gb", true);
        // game = new Cartridge("roms/05-op rp.gb", true);
        // game = new Cartridge("roms/06-ld r,r.gb", true);
        // game = new Cartridge("roms/07-jr,jp,call,ret,rst.gb", true);
        // game = new Cartridge("roms/08-misc instrs.gb", true);
        // game = new Cartridge("roms/09-op r,r.gb", true);
        // game = new Cartridge("roms/10-bit ops.gb", true);
        // game = new Cartridge("roms/11-op a,(hl).gb", true);
        game = new Cartridge("roms/cpu_instrs.gb", true);
        // game = new Cartridge("roms/cgb_sound.gb", true);
        // game = new Cartridge("roms/Pokemon Red.gb", true);

        //Connect it all to the bus
        cpu.Connect(bus);
        controller.Connect(bus);
        timer.Connect(bus);
        lcd.Connect(bus);
        bus.ConnectCartridge(game);
    }

    public void Play()
    {
        bool frameDrawn = false;
        controller.CheckInputs();
        while (!frameDrawn)
        {
            ulong cpuCycles = cpu.Tick();
            timer.Tick(cpuCycles);
            frameDrawn = lcd.Tick(cpuCycles);
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