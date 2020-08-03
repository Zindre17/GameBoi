using System.Threading;
using System.Threading.Tasks;

abstract class Hardware
{
    protected Bus bus;

    public virtual void Connect(Bus bus) => this.bus = bus;

    public virtual Byte Read(Address address) => bus.Read(address);
    public virtual void Write(Address address, Byte value) => bus.Write(address, value);

    public abstract void Tick();

    public ulong Cycles => bus.Cycles;

    protected Task runner;
    protected bool isRunning = false;

    public virtual void Loop()
    {
        Tick();
    }
    public void Run()
    {
        if (runner != null) runner.Dispose();

        runner = new Task(() =>
        {
            while (isRunning)
            {
                Loop();
            }
        });

        isRunning = true;
        runner.Start();
    }

    public void Stop()
    {
        isRunning = false;
    }

}