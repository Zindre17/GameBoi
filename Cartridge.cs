class Cartridge : Hardware<MainBus>
{
    public ROM ROM_Bank0 { get; private set; } = new ROM(0x4000); //16KB
    public Memory ROM_BankN { get; private set; } //Can be both ROM and RAM
    public RAM RAM { get; private set; } // can be null;

    public Cartridge(string pathToROM)
    {

        //read rom file

        //get info from header

        //determine if it has a Memory Bank Controller(MBC) => RAM:ROM

        //determine if it has RAM
    }

    // Ehhh... not so elegant...
    public override void Connect(MainBus bus)
    {
        base.Connect(bus);
        bus.ConnectCartridge(this);
    }
}