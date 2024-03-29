using System;
using System.Collections.Generic;
using System.Windows.Media;
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

        private Cartridge game;

        private readonly List<LoopRunner> loops = new();

        public Gameboi()
        {
            bus.Connect(cpu);
            bus.Connect(spu);
            bus.Connect(timer);
            bus.Connect(controller);
            bus.Connect(dma);
            bus.Connect(lcd);

            loops.Add(new LoopRunner(cpu));
            loops.Add(new LoopRunner(spu));
        }

        public void Play()
        {
            if (isOn)
            {
                foreach (var loop in loops)
                {
                    loop.Start();
                }
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

        public string LoadGame(string path)
        {
            if (game != null)
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

        public ImageSource GetScreen() => lcd.Screen;

        public void CheckController() => controller.RegisterInputs();
        public void Render() => lcd.DrawFrame();

        private bool isOn = false;
        public void TurnOn() => isOn = game != null;
        public void TurnOff()
        {
            if (isOn)
            {
                isOn = false;
                game.CloseFileStream();
            }
        }

    }
}