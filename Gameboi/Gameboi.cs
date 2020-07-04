using System;

class GameBoy
{
    private CPU cpu;
    private Timer timer;
    public LCD lcd;
    private Controller controller;
    private Cartridge game;
    private Bus bus;

    public GameBoy()
    {
        // Create hardware
        bus = new Bus();
        cpu = new CPU();
        timer = new Timer();
        lcd = new LCD();
        controller = new Controller();
        // game = Cartridge.LoadGame("roms/blargg/01-special.gb");
        // game = Cartridge.LoadGame("roms/blargg/02-interrupts.gb");
        // game = Cartridge.LoadGame("roms/blargg/03-op sp,hl.gb");
        // game = Cartridge.LoadGame("roms/blargg/04-op r,imm.gb");
        // game = Cartridge.LoadGame("roms/blargg/05-op rp.gb");
        // game = Cartridge.LoadGame("roms/blargg/06-ld r,r.gb");
        // game = Cartridge.LoadGame("roms/blargg/07-jr,jp,call,ret,rst.gb");
        // game = Cartridge.LoadGame("roms/blargg/08-misc instrs.gb");
        // game = Cartridge.LoadGame("roms/blargg/09-op r,r.gb");
        // game = Cartridge.LoadGame("roms/blargg/10-bit ops.gb");
        // game = Cartridge.LoadGame("roms/blargg/11-op a,(hl).gb");
        // game = Cartridge.LoadGame("roms/blargg/cpu_instrs.gb");
        // game = Cartridge.LoadGame("roms/blargg/instr_timing.gb");
        // game = Cartridge.LoadGame("roms/blargg/mem_timing1.gb");
        // game = Cartridge.LoadGame("roms/blargg/mem_timing2.gb");

        // game = Cartridge.LoadGame("roms/acceptance/bits/mem_oam.gb"); // OK
        // game = Cartridge.LoadGame("roms/acceptance/bits/reg_f.gb"); // OK
        // game = Cartridge.LoadGame("roms/acceptance/bits/unused_hwio-GS.gb"); // Fails on unused bits in IO registers not being 1

        // game = Cartridge.LoadGame("roms/acceptance/instr/daa.gb"); //OK

        // game = Cartridge.LoadGame("roms/acceptance/interrupts/ie_push.gb"); // R1: not cancelled

        // game = Cartridge.LoadGame("roms/acceptance/oam_dma/basic.gb"); // OK
        // game = Cartridge.LoadGame("roms/acceptance/oam_dma/reg_read.gb"); // Fail: r1
        // game = Cartridge.LoadGame("roms/acceptance/oam_dma/sources-GS.gb"); // crash, cart type not implemented

        // game = Cartridge.LoadGame("roms/acceptance/ppu/hblank_ly_scx_timing-GS.gb"); // blank screen
        // game = Cartridge.LoadGame("roms/acceptance/ppu/intr_1_2_timing-GS.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/intr_2_0_timing.gb"); 
        // game = Cartridge.LoadGame("roms/acceptance/ppu/intr_2_mode0_timing.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/intr_2_mode0_timing_sprites.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/intr_2_mode3_timing.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/intr_2_oam_ok_timing.gb");

        // game = Cartridge.LoadGame("roms/acceptance/ppu/lcdon_timing-GS.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/lcdon_write_timing-GS.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/stat_irq_blocking.gb");
        // game = Cartridge.LoadGame("roms/acceptance/ppu/stat_lyc_onoff.gb");

        // game = Cartridge.LoadGame("roms/acceptance/ppu/vblank_stat_intr-GS.gb");

        // game = Cartridge.LoadGame("roms/acceptance/timer/div_write.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/rapid_toggle.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim00.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim00_div_trigger.gb"); // OK
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim01.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim01_div_trigger.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim10.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim10_div_trigger.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim11.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tim11_div_trigger.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tima_reload.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tima_write_reloading.gb");
        // game = Cartridge.LoadGame("roms/acceptance/timer/tma_write_reloading.gb");

        // game = Cartridge.LoadGame("roms/acceptance/add_sp_e_timing.gb");

        // game = Cartridge.LoadGame("roms/cgb_sound.gb");
        // game = Cartridge.LoadGame("roms/Pokemon Red.gb");
        // game = Cartridge.LoadGame("roms/Tetris (JUE) (V1.1) [!].gb");
        // game = Cartridge.LoadGame("roms/Super Mario Land 2 - 6 Golden Coins (UE) (V1.2) [!].gb");

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

}