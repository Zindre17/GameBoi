using System;

namespace Gameboi.Cartridges;

public interface IMemoryBankControllerLogic
{
    ref byte GetRamRef(byte[] ram, ushort address);
    ref byte GetRomRef(byte[] rom, ushort address);
    byte ReadRam(byte[] ram, ushort address);
    byte ReadRom(byte[] rom, ushort address);
    void WriteRam(byte[] ram, ushort address, byte value);
    void WriteRom(byte[] rom, ushort address, byte value);
}

public class NoMemoryBankController : IMemoryBankControllerLogic
{
    public ref byte GetRamRef(byte[] ram, ushort address)
    {
        return ref ram[address % ram.Length];
    }

    public ref byte GetRomRef(byte[] rom, ushort address)
    {
        return ref rom[address % rom.Length];
    }

    public byte ReadRam(byte[] ram, ushort address)
    {
        return ram[address % ram.Length];
    }

    public byte ReadRom(byte[] rom, ushort address)
    {
        return rom[address];
    }

    public void WriteRam(byte[] ram, ushort address, byte value)
    {
        ram[address % ram.Length] = value;
    }

    public void WriteRom(byte[] rom, ushort address, byte value)
    { }
}

public abstract class MemoryBankControllerBase : IMemoryBankControllerLogic
{
    protected const int RomBankSize = 0x4_000;
    protected const int RamBankSize = 0x2_000;

    protected const byte Enabled = 0x00;
    protected const byte Disabled = 0xff;

    protected byte ramState = Disabled;
    protected int ramOffset = 0;

    protected int rom0Offset = 0;
    protected int rom1Offset = RomBankSize;

    public ref byte GetRamRef(byte[] ram, ushort address)
    {
        return ref ram[ramOffset + address];
    }

    public ref byte GetRomRef(byte[] rom, ushort address)
    {
        if (address < RomBankSize)
        {
            return ref rom[rom0Offset + address];
        }
        return ref rom[rom1Offset + address];
    }

    public virtual byte ReadRam(byte[] ram, ushort address)
    {
        return (byte)(ram[ramOffset + address] | ramState);
    }

    public virtual byte ReadRom(byte[] rom, ushort address)
    {
        return address switch
        {
            < RomBankSize => rom[rom0Offset + address],  // Slot 0
            _ => rom[rom1Offset + address - RomBankSize] // Slot 1
        };
    }

    public virtual void WriteRam(byte[] ram, ushort address, byte value)
    {
        // ramState Enabled : value & ff
        // ramState Disabled : ff & current value
        ram[ramOffset + address] = (byte)(
            (value | ramState)
            & (ram[ramOffset + address] | ~ramState)
        );
    }

    protected static int RomBanks(int length) => length / RomBankSize;
    protected static int RamBanks(int length) => length / RamBankSize;

    public abstract void WriteRom(byte[] rom, ushort address, byte value);
}

public class MemoryBankController1 : MemoryBankControllerBase
{
    private int romSelectRegister;
    private int ramSelectRegister;
    private int mode;

    public override void WriteRom(byte[] rom, ushort address, byte value)
    {
        switch (address)
        {
            case < 0x2000:
                ramState = (value & 0xF) is 0xA ? Enabled : Disabled;
                break;
            case < 0x4000:
                romSelectRegister = value & 0x1F;
                break;
            case < 0x6000:
                ramSelectRegister = value & 3;
                break;
            default:
                mode = value & 1;
                break;
        }
        UpdateBankSelects(RomBanks(rom.Length));
    }

    private void UpdateBankSelects(int romBanks)
    {
        ramOffset = mode * ramSelectRegister * RamBankSize;

        var romSlot1Select = ((1 - mode) * (ramSelectRegister << 5)) | romSelectRegister;
        if (romSlot1Select is 0 or 0x20 or 0x40 or 0x60)
        {
            romSlot1Select++;
        }
        rom1Offset = romSlot1Select * RomBankSize;

        // This is a bug in the hardware AFIAK
        if (mode == 1)
        {
            rom0Offset = (ramSelectRegister << 5) % romBanks * RomBankSize;
        }
        else
        {
            rom0Offset = 0;
        }
    }
}

public class MemoryBankController2 : MemoryBankControllerBase
{
    public override byte ReadRam(byte[] ram, ushort address)
    {
        return (byte)(ram[address % ram.Length] | ramState | 0xF0);
    }

    public override void WriteRom(byte[] rom, ushort address, byte value)
    {
        if (address < 0x4000)
        {
            if ((address & 0x0100) is 0x0100)
            {
                var romSelect = (value & 0x0F) % RomBanks(rom.Length);
                if (romSelect is 0)
                {
                    romSelect = 1;
                }
                rom1Offset = romSelect * RomBankSize;
            }
            else
            {
                ramState = (value & 0b0101) is 0b0101
                    ? Enabled : Disabled;
            }
        }
    }
}

public class MemoryBankController3 : MemoryBankControllerBase
{
    private int ramSelect = 0;

    public override byte ReadRam(byte[] ram, ushort address)
    {
        return ramSelect switch
        {
            < 8 => (byte)(ram[ramOffset + address] | ramState),
            8 => (byte)(DateTime.Now.Second | ramState),
            9 => (byte)(DateTime.Now.Minute | ramState),
            0xA => (byte)(DateTime.Now.Hour | ramState),
            0xB => (byte)((int)DateTime.Now.DayOfWeek | ramState),
            0xC => ramState,
            _ => throw new Exception("invalid ram select.")
        };
    }

    public override void WriteRom(byte[] rom, ushort address, byte value)
    {
        if (address < 0x2000)
        {
            if (value is 0x0A)
            {
                ramState = Enabled;
            }
            else if (value is 0)
            {
                ramState = Disabled;
            }
        }
        else if (address < 0x4000)
        {
            var romSelect = (value & 0x7F) % RomBanks(rom.Length);
            if (romSelect is 0)
            {
                romSelect = 1;
            }
            rom1Offset = romSelect * RomBankSize;
        }
        else if (address < 0x6000)
        {
            ramSelect = value % 0x0D;
        }
    }
}

public class MemoryBankController5 : MemoryBankControllerBase
{
    private int romSelectLow = 1;
    private int romSelectHigh = 0;

    public override void WriteRom(byte[] rom, ushort address, byte value)
    {
        if (address < 0x2000)
        {
            if (value is 0x0A)
            {
                ramState = Enabled;
            }
            else if (value is 0)
            {
                ramState = Disabled;
            }
        }
        else if (address < 0x3000)
        {
            romSelectLow = value;
        }
        else if (address < 0x4000)
        {
            romSelectHigh = value;
        }
        else if (address < 0x6000)
        {
            ramOffset = (value & 0xF) * RamBankSize;
        }

        var romSelect = (romSelectHigh << 8 | romSelectLow) % RomBanks(rom.Length);
        rom1Offset = romSelect * RomBankSize;
    }
}
