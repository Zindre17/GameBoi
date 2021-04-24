
using System;
using System.Windows.Media;

public class Gameboi
{
    private readonly CPU cpu = new CPU();
    private readonly LCD lcd = new LCD();
    private readonly Controller controller = new Controller();
    private readonly DMA dma = new DMA();
    private readonly SPU spu = new SPU();
    private readonly Bus bus = new Bus();
    private readonly Timer timer = new Timer();

    private Cartridge game;


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
            cpu.Run();
    }

    public void Pause()
    {
        if (isOn)
            cpu.Pause();
    }

    public void PausePlayToggle()
    {
        if (isOn)
            if (cpu.IsRunning)
                cpu.Pause();
            else
                cpu.Run();
    }

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
            bus.ConnectCartridge(game);
            cpu.Restart();
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