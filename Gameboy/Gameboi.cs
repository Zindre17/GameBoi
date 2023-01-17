using System;
using GB_Emulator.Cartridges;
using GB_Emulator.Graphics;
using GB_Emulator.Hardware;
using GB_Emulator.Sound;

namespace GB_Emulator
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

        public Gameboi()
        {
            lcd.OnLineLoaded += (line, data) =>
            {
                OnPixelRowReady?.Invoke(line, data);
            };
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
                isPaused = false;
            }
        }

        public void Pause()
        {
            if (isOn)
            {
                isPaused = true;
            }

        }

        public void PausePlayToggle()
        {
            if (isOn)
            {
                isPaused = !isPaused;
            }
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

        public Controller Controller => controller;

        public void PlayForOneFrame()
        {
            cpu.Loop(0);
            spu.Loop(0);
        }

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

        public Action<byte, Rgba[]>? OnPixelRowReady;
    }
}
