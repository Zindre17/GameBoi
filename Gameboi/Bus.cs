using System;
using Gameboi.Cartridges;

namespace Gameboi;

public class Bus
{
    private readonly SystemState state;
    private readonly IoLogic ioLogic;
    private readonly IMemoryBankControllerLogic mbc;

    public Bus(SystemState state, IMemoryBankControllerLogic mbcLogic)
    {
        this.state = state;
        ioLogic = new(state);
        mbc = mbcLogic;
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            < 0x8000 => mbc.ReadRom(address),
            < 0xA000 => state.VideoRam[state.VideoRamOffset + address - 0x8000],
            < 0xC000 => mbc.ReadRam((ushort)(address - 0xA000)),
            < 0xD000 => state.WorkRam[address - 0xC000],
            < 0xE000 => state.WorkRam[state.WorkRamOffset + address - 0xD000],
            < 0xF000 => state.WorkRam[address - 0xE000], // ECHO 0xC000 - 0xCFFF
            < 0xFE00 => state.WorkRam[state.WorkRamOffset + address - 0xF000], // ECHO 0xD000 - 0xDDFF
            < 0xFEA0 => state.Oam[address - 0xFE00],
            < 0xFF00 => 0xff, // Not used
            < 0xFF80 => ioLogic.Read((ushort)(address - 0xFF00)),
            < 0xFFFF => state.HighRam[address - 0xFF80],
            _ => state.InterruptEnableRegister
        };
    }

    public ref byte GetRef(ushort address)
    {
        switch (address)
        {
            case < 0x8000:
                return ref mbc.GetRomRef(address);
            case < 0xA000:
                return ref state.VideoRam[state.VideoRamOffset + address - 0x8000];
            case < 0xC000:
                return ref mbc.GetRamRef((ushort)(address - 0xA000));
            case < 0xD000:
                return ref state.WorkRam[address - 0xC000];
            case < 0xE000:
                return ref state.WorkRam[state.WorkRamOffset + address - 0xD000];
            case < 0xF000:
                return ref state.WorkRam[address - 0xE000]; // ECHO 0xC000 - 0xCFFF
            case < 0xFE00:
                return ref state.WorkRam[state.WorkRamOffset + address - 0xF000]; // ECHO 0xD000 - 0xDDFF
            case < 0xFEA0:
                return ref state.Oam[address - 0xFE00];
            case < 0xFF00:
                throw new Exception(); // Not used
            case < 0xFF80:
                return ref ioLogic.GetRef((ushort)(address - 0xFF00));
            case < 0xFFFF:
                return ref state.HighRam[address - 0xFF80];
            default:
                return ref state.InterruptEnableRegister;
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x8000:
                mbc.WriteRom(address, value);
                break;
            case < 0xA000:
                state.VideoRam[state.VideoRamOffset + address - 0x8000] = value;
                break;
            case < 0xC000:
                mbc.WriteRam((ushort)(address - 0xA000), value);
                break;
            case < 0xD000:
                state.WorkRam[address - 0xC000] = value;
                break;
            case < 0xE000:
                state.WorkRam[state.WorkRamOffset + address - 0xD000] = value;
                break;
            case < 0xF000:
                state.WorkRam[address - 0xE000] = value;
                break;
            case < 0xFE00:
                state.WorkRam[state.WorkRamOffset + address - 0xF000] = value;
                break;
            case < 0xFEA0:
                state.Oam[address - 0xFE00] = value;
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


