using System;
using Gameboi.Extensions;
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
        if (state.CartridgeRam.Length is 0)
        {
            return 0xff;
        }
        return state.CartridgeRam[address % state.CartridgeRam.Length];
    }

    public byte ReadRom(ushort address)
    {
        return state.CartridgeRom[address];
    }

    public void WriteRam(ushort address, byte value)
    {
        if (state.CartridgeRam.Length > 0)
        {
            state.CartridgeRam[address % state.CartridgeRam.Length] = value;
        }
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

    protected int RomBankMask => RomBanks - 1;
    protected int RamBankMask => RamBanks - 1;

    public abstract void WriteRom(ushort address, byte value);
}

public class MemoryBankController1 : MemoryBankControllerBase
{
    // TODO: Emulate bug where bank 0 can appear at 0x4000-0x7fff
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
            case < 0x8000:
                state.MbcMode = value & 1;
                break;
        }

        UpdateBankSelects();
    }

    private void UpdateBankSelects()
    {
        int romBankNr;
        if (state.MbcMode is 0)
        {
            state.MbcRamOffset = 0;
            romBankNr = (state.MbcRamSelect << 5) | state.MbcRomSelectLow;
            state.MbcRom0Offset = 0;
        }
        else
        {
            if (RamBanks is not 0)
            {
                state.MbcRamOffset = (state.MbcRamSelect & RamBankMask) * RamBankSize;
            }
            romBankNr = state.MbcRomSelectLow;
            state.MbcRom0Offset = ((state.MbcRamSelect << 5) & RomBankMask) * RomBankSize;
        }

        if (romBankNr is 0 or 0x20 or 0x40 or 0x60)
        {
            romBankNr += 1;
        }
        state.MbcRom1Offset = (romBankNr & RomBankMask) * RomBankSize;
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
        address &= 0x01ff;
        return (byte)(state.CartridgeRam[address] | 0xF0);
    }

    public override void WriteRom(ushort address, byte value)
    {
        if (address < 0x4000)
        {
            if (address.GetHighByte().IsBitSet(0))
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
                var enabled = value is 0x0a;
                state.MbcRamDisabled = !enabled;
            }
        }
    }
}

public class MemoryBankController3 : MemoryBankControllerBase
{
    public MemoryBankController3(SystemState state) : base(state) { }

    // TODO: Implement latching of RTC registers
    // TODO: Fix day counter
    public override byte ReadRam(ushort address)
    {
        if (state.MbcRamDisabled || state.CartridgeRam.Length is 0)
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
            state.MbcRomSelectHigh = value & 1;
        }
        else if (address < 0x6000)
        {
            state.MbcRamOffset = (value & 0xF) * RamBankSize;
        }

        var romSelect = (state.MbcRomSelectHigh << 8 | state.MbcRomSelectLow) % RomBanks;
        state.MbcRom1Offset = romSelect * RomBankSize;
    }
}
