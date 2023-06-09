using System;
using Gameboi.Cartridges;
using Gameboi.Graphics;
using Gameboi.Hardware;

namespace Gameboi;

public class Gameboy
{
    private readonly CPU cpu = new();
    private readonly DMA dma = new();
    private readonly Bus bus = new();
    private readonly Timer timer = new();

    private Cartridge? game;

    public Gameboy()
    {
        bus.Connect(cpu);
        bus.Connect(timer);
        bus.Connect(dma);
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
            game.Connect(bus);
            cpu.Restart(game.IsColorGame);
            TurnOn();
            Play();
            return game.Title;
        }
        catch (Exception) { }
        return null;
    }

    public void PlayForOneFrame()
    {
        cpu.Loop(0);
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

