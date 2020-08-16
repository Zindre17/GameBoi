
using System;
using System.Windows.Media;

class Gameboi
{
    private CPU cpu = new CPU();
    private LCD lcd = new LCD();
    private Controller controller = new Controller();
    private DMA dma = new DMA();
    private SPU spu = new SPU();
    private Bus bus = new Bus();
    private Timer timer = new Timer();

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

    public void LoadGame(string path)
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
        }
        catch (Exception) { }
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