using Gameboi.Cartridges;

namespace Gameboi.Hardware;

public class ImprovedBus
{
    private readonly SystemState state;
    private readonly IoLogic ioLogic;
    private readonly IMemoryBankControllerLogic mbc;

    public ImprovedBus(SystemState state, IMemoryBankControllerLogic mbcLogic)
    {
        this.state = state;
        ioLogic = new(state);
        mbc = mbcLogic;
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            < 0x8000 => mbc.ReadRom(state.CartridgeRom, address),
            < 0xA000 => state.VideoRam[address - 0x8000],
            < 0xC000 => mbc.ReadRam(state.CartridgeRam, (ushort)(address - 0xA000)),
            < 0xE000 => state.WorkRam[address - 0xC000],
            < 0xFE00 => state.WorkRam[address - 0xE000], // ECHO 0xC000 - 0xDDFF
            < 0xFF00 => 0xff, // Not used
            < 0xFF80 => ioLogic.Read((ushort)(address - 0xFF00)),
            < 0xFFFF => state.HighRam[address - 0xFF80],
            _ => state.InterruptEnableRegister
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x8000:
                mbc.WriteRom(state.CartridgeRom, address, value);
                break;
            case < 0xA000:
                state.VideoRam[address - 0x8000] = value;
                break;
            case < 0xC000:
                mbc.WriteRam(state.CartridgeRam, (ushort)(address - 0xA000), value);
                break;
            case < 0xE000:
                state.WorkRam[address - 0xC000] = value;
                break;
            case < 0xFE00:
                state.WorkRam[address - 0xE000] = value;
                break;
            case < 0xFF00: break; // Not used
            case < 0xFF80:
                ioLogic.Write((ushort)(address - 0xFF00), value);
                break;
            case < 0xFFFF:
                state.HighRam[address - 0xFF80] = value;
                break;
            default:
                state.InterruptEnableRegister = value;
                break;
        };
    }
}

