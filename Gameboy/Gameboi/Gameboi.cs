using System;
using System.Collections.Generic;
using GB_Emulator.Cartridges;
using GB_Emulator.Gameboi.Hardware;
using GB_Emulator.Sound;

namespace GB_Emulator.Gameboi
{
    public class Gameboi
    {
        private readonly CPU cpu = new();
        private readonly LCD lcd = new();
        private readonly Controller controller = new();
        private readonly DMA dma = new();
        private readonly SPU spu = new();
        private readonly Bus bus = new();
        private readonly Timer timer = new();

        private Cartridge? game;

        private readonly List<LoopRunner> loops = new();

        public Gameboi()
        {
            bus.Connect(cpu);
            bus.Connect(spu);
            bus.Connect(timer);
            bus.Connect(controller);
            bus.Connect(dma);
            bus.Connect(lcd);
        }

        public void Play()
        {
            if (isOn)
            {
                foreach (var loop in loops)
                {
                    loop.Start();
                }
                isPaused = false;
            }
        }

        public void Pause()
        {
            if (isOn)
            {
                foreach (var loop in loops)
                {
                    loop.Stop();
                }
                isPaused = true;
            }

        }

        public void PausePlayToggle()
        {
            if (isOn)
            {
                foreach (var loop in loops)
                {
                    loop.Toggle();
                }
            }
        }

        public void ToggleBackground()
        {
            lcd.ToggleBackground();
        }
        public void ToggleWindow()
        {
            lcd.ToggleWindow();
        }
        public void ToggleSprites()
        {
            lcd.ToggleSprites();
        }

        public void ChangeSpeed(bool faster)
        {
            cpu.ChangeSpeed(faster);
        }

        public void ChangeVolume(bool up)
        {
            if (up)
            {
                spu.VolumeUp();
            }
            else
            {
                spu.VolumeDown();
            }
        }

        public void ToggleMute() => spu.ToggleMute();

        public string? LoadGame(string path)
        {
            if (game is not null)
            {
                Pause();
                TurnOff();
            }
            try
            {
                game = Cartridge.LoadGame(path);
                lcd.UseColorScreen(game.IsColorGame);
                game.Connect(bus);
                cpu.Restart(game.IsColorGame);
                TurnOn();
                Play();
                return game.Title;
            }
            catch (Exception) { }
            return null;
        }

        public LCD Screen => lcd;
        public Controller Controller => controller;
        public CPU Cpu => cpu;
        public SPU Spu => spu;

        private bool isOn = false;
        private bool isPaused = false;
        public bool IsPlaying => isOn && !isPaused;
        public void TurnOn() => isOn = game != null;
        public void TurnOff()
        {
            if (isOn)
            {
                isOn = false;
                game?.CloseFileStream();
            }
        }

    }
}
