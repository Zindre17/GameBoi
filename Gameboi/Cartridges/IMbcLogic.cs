using System;
using static Gameboi.MbcCartridgeBankSizes;

namespace Gameboi.Cartridges;

public enum MbcType
{
    NoMbc,
    Mbc1,
    Mbc2,
    Mbc3,
    Mbc5
}

public static class MbcFactory
{
    public static IMemoryBankControllerLogic GetMbcLogic(MbcType type, SystemState state)
    {
        return type switch
        {
            MbcType.NoMbc => new NoMemoryBankController(state),
            MbcType.Mbc1 => new MemoryBankController1(state),
            MbcType.Mbc2 => new MemoryBankController2(state),
            MbcType.Mbc3 => new MemoryBankController3(state),
            MbcType.Mbc5 => new MemoryBankController5(state),
            _ => throw new Exception("Invalid cartridge type"),
        };
    }
}

public interface IMemoryBankControllerLogic
{
    ref byte GetRamRef(ushort address);
    ref byte GetRomRef(ushort address);
    byte ReadRam(ushort address);
    byte ReadRom(ushort address);
    void WriteRam(ushort address, byte value);
    void WriteRom(ushort address, byte value);
}

public class NoMemoryBankController : IMemoryBankControllerLogic
{
    private readonly SystemState state;

    public NoMemoryBankController(SystemState state) => this.state = state;

    public ref byte GetRamRef(ushort address)
    {
        return ref state.CartridgeRam[address % state.CartridgeRam.Length];
    }

    public ref byte GetRomRef(ushort address)
    {
        return ref state.CartridgeRom[address % state.CartridgeRom.Length];
    }

    public byte ReadRam(ushort address)
    {
        return state.CartridgeRam[address % state.CartridgeRam.Length];
    }

    public byte ReadRom(ushort address)
    {
        return state.CartridgeRom[address];
    }

    public void WriteRam(ushort address, byte value)
    {
        state.CartridgeRam[address % state.CartridgeRam.Length] = value;
    }

    public void WriteRom(ushort address, byte value)
    { }
}

public abstract class MemoryBankControllerBase : IMemoryBankControllerLogic
{
    protected readonly SystemState state;

    public MemoryBankControllerBase(SystemState state) => this.state = state;

    public ref byte GetRamRef(ushort address)
    {
        return ref state.CartridgeRam[state.MbcRamOffset + address];
    }

    public ref byte GetRomRef(ushort address)
    {
        if (address < RomBankSize)
        {
            return ref state.CartridgeRom[state.MbcRom0Offset + address];
        }
        return ref state.CartridgeRom[state.MbcRom1Offset + address];
    }

    public virtual byte ReadRam(ushort address)
    {
        if (state.MbcRamDisabled || state.CartridgeRam.Length is 0)
        {
            return 0xff;
        }
        return state.CartridgeRam[state.MbcRamOffset + address];
    }

    public virtual byte ReadRom(ushort address)
    {
        return address switch
        {
            < RomBankSize => state.CartridgeRom[state.MbcRom0Offset + address],  // Slot 0
            _ => state.CartridgeRom[state.MbcRom1Offset + address - RomBankSize] // Slot 1
        };
    }

    public virtual void WriteRam(ushort address, byte value)
    {
        if (state.MbcRamDisabled || state.CartridgeRam.Length is 0)
        {
            return;
        }
        state.CartridgeRam[state.MbcRamOffset + address] = value;
    }

    protected int RomBanks => state.CartridgeRom.Length / RomBankSize;
    protected int RamBanks => state.CartridgeRam.Length / RamBankSize;

    public abstract void WriteRom(ushort address, byte value);
}

public class MemoryBankController1 : MemoryBankControllerBase
{
    public MemoryBankController1(SystemState state) : base(state) { }

    public override void WriteRom(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x2000:
                var enabled = (value & 0xF) is 0xA;
                state.MbcRamDisabled = !enabled;
                break;
            case < 0x4000:
                state.MbcRomSelectLow = value & 0x1F;
                break;
            case < 0x6000:
                state.MbcRamSelect = value & 3;
                break;
            default:
                state.MbcMode = value & 1;
                break;
        }

        UpdateBankSelects();
    }

    private void UpdateBankSelects()
    {
        state.MbcRamOffset = state.MbcMode * state.MbcRamSelect * RamBankSize;

        var romSlot1Select = ((1 - state.MbcMode) * (state.MbcRamSelect << 5)) | state.MbcRomSelectLow;
        if (romSlot1Select is 0 or 0x20 or 0x40 or 0x60)
        {
            romSlot1Select++;
        }
        state.MbcRom1Offset = romSlot1Select * RomBankSize;

        // This is a bug in the hardware AFIAK
        if (state.MbcMode is 1)
        {
            state.MbcRom0Offset = (state.MbcRamSelect << 5) % RomBanks * RomBankSize;
        }
        else
        {
            state.MbcRom0Offset = 0;
        }
    }
}

public class MemoryBankController2 : MemoryBankControllerBase
{
    public MemoryBankController2(SystemState state) : base(state) { }

    public override byte ReadRam(ushort address)
    {
        if (state.MbcRamDisabled)
        {
            return 0xff;
        }
        return (byte)(state.CartridgeRam[address % state.CartridgeRam.Length] | 0xF0);
    }

    public override void WriteRom(ushort address, byte value)
    {
        if (address < 0x4000)
        {
            if ((address & 0x0100) is 0x0100)
            {
                var romSelect = (value & 0x0F) % RomBanks;
                if (romSelect is 0)
                {
                    romSelect = 1;
                }
                state.MbcRom1Offset = romSelect * RomBankSize;
            }
            else
            {
                var enabled = (value & 0b0101) is 0b0101;
                state.MbcRamDisabled = !enabled;
            }
        }
    }
}

public class MemoryBankController3 : MemoryBankControllerBase
{
    public MemoryBankController3(SystemState state) : base(state) { }

    public override byte ReadRam(ushort address)
    {
        if (state.MbcRamDisabled)
        {
            return 0xff;
        }
        return state.MbcRamSelect switch
        {
            < 8 => state.CartridgeRam[state.MbcRamOffset + address],
            8 => (byte)DateTime.Now.Second,
            9 => (byte)DateTime.Now.Minute,
            0xA => (byte)DateTime.Now.Hour,
            0xB => (byte)DateTime.Now.DayOfWeek,
            0xC => 0,
            _ => throw new Exception("invalid ram select.")
        };
    }

    public override void WriteRom(ushort address, byte value)
    {
        if (address < 0x2000)
        {
            if (value is 0x0A)
            {
                state.MbcRamDisabled = false;
            }
            else if (value is 0)
            {
                state.MbcRamDisabled = true;
            }
        }
        else if (address < 0x4000)
        {
            var romSelect = (value & 0x7F) % RomBanks;
            if (romSelect is 0)
            {
                romSelect = 1;
            }
            state.MbcRom1Offset = romSelect * RomBankSize;
        }
        else if (address < 0x6000)
        {
            state.MbcRamSelect = value % 0x0D;
        }
    }
}

public class MemoryBankController5 : MemoryBankControllerBase
{
    public MemoryBankController5(SystemState state) : base(state) { }

    public override void WriteRom(ushort address, byte value)
    {
        if (address < 0x2000)
        {
            if (value is 0x0A)
            {
                state.MbcRamDisabled = false;
            }
            else if (value is 0)
            {
                state.MbcRamDisabled = true;
            }
        }
        else if (address < 0x3000)
        {
            state.MbcRomSelectLow = value;
        }
        else if (address < 0x4000)
        {
            state.MbcRomSelectHigh = value;
        }
        else if (address < 0x6000)
        {
            state.MbcRamOffset = (value & 0xF) * RamBankSize;
        }

        var romSelect = (state.MbcRomSelectHigh << 8 | state.MbcRomSelectLow) % RomBanks;
        state.MbcRom1Offset = romSelect * RomBankSize;
    }
}
