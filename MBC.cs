using System;
using static ByteOperations;


class NoMBC : IMemory
{
    IMemory ROM;
    IMemory RAM;

    private bool hasRAM = false;
    private bool hasBattery = false;

    public NoMBC(bool hasRAM, byte[] cartridgeData, byte[] batteryStoredRAM = null)
    {
        this.hasRAM = hasRAM;

        if (batteryStoredRAM != null)
            hasBattery = true;

        ROM = new Memory(Cartridge.GetCartridgeChunk(0, 0x8000, cartridgeData), true);

        if (hasRAM)
        {
            if (hasBattery)
            {
                throw new NotImplementedException();
            }
            else
            {
                RAM = new Memory(0x2000);
            }
        }
    }

    public bool Read(Address address, out Byte value)
    {
        if (address < 0x8000)
            return ROM.Read(address, out value);
        else if (hasRAM && address >= 0xA000 && address < 0xC000)
            return RAM.Read(address, out value);
        throw new NoMemoryAtLocationException(address);
    }

    public bool Write(Address address, Byte value)
    {
        if (!hasRAM || address < 0xA000 || address > 0xC000)
            throw new ArgumentOutOfRangeException();
        return RAM.Write(address - 0xA000, value);
    }

}
class MBC1 : IMemory
{
    private IMemory ROMBank0;
    private Bank ROMBankN;
    private Bank RAMBankN;

    private Byte mode = 0;
    private bool isRamEnabled = false;

    private Byte ROMBankNumber;
    private Byte RAMBankNumber;
    private const ushort ROMSizePerBank = 0x4000;

    private Address BankNStart = 0x4000;
    private Address BankNEnd = 0x8000;
    private Address RAMStart = 0xA000;
    private Address RAMEnd = 0xC000;

    public MBC1(bool hasRAM, ushort ROMbanks, ushort RAMSize, byte[] cartridgeData)
    {
        ROMBank0 = new Memory(Cartridge.GetCartridgeChunk(0, ROMSizePerBank, cartridgeData), true);

        // adjust for "holes" in bankspace at(0x20, 0x40, 0x60)
        if (ROMbanks == 63)
        {
            ROMbanks--;
        }
        else if (ROMbanks == 127)
        {
            ROMbanks -= 3;
        }

        IMemory[] switchableBanks = new IMemory[ROMbanks];
        for (int i = 0; i < ROMbanks; i++)
        {
            int startAddress = ROMSizePerBank * (i + 1);
            switchableBanks[i] = new Memory(Cartridge.GetCartridgeChunk(startAddress, ROMSizePerBank, cartridgeData), true);
        }
        ROMBankN = new Bank(switchableBanks);

        if (hasRAM)
        {
            byte count;
            ushort size;
            if (RAMSize == 0x8000)
            {
                count = 4;
                size = 0x2000;
            }
            else
            {
                count = 1;
                size = RAMSize;
            }
            RAMBankN = new Bank(count, size);
        }
    }
    public bool Read(Address address, out Byte value)
    {
        if (address < BankNStart)
            return ROMBank0.Read(address, out value);
        else if (address < BankNEnd)
            return ROMBankN.Read(address - BankNStart, out value);
        else if (address >= RAMStart && address < RAMEnd)
            return RAMBankN.Read(address - RAMStart, out value);
        else throw new ArgumentOutOfRangeException();
    }

    private Address ToggleRAMEnd = 0x2000;
    private Address SwitchROMEnd = 0x4000;
    private Address SwitchRAMEnd = 0x6000;
    private Address ToggleModeEnd = 0x8000;
    public bool Write(Address address, Byte value)
    {
        if (address < ToggleRAMEnd)
        {
            ToggleRAM(value);
        }
        else if (address < SwitchROMEnd)
        {
            SetROMBankNr(value);
        }
        else if (address < SwitchRAMEnd)
        {
            SetRAMBankNr(value);
        }
        else if (address < ToggleModeEnd)
        {
            ToggleMode(value);
        }
        else if (address >= RAMStart && address < RAMEnd)
        {
            if (isRamEnabled)
                return RAMBankN.Write(address - RAMStart, value);
            return false;
        }
        else return false;

        return true;
    }

    private void ToggleRAM(Byte value)
    {
        isRamEnabled = value == 0x0A;
    }

    private void ToggleMode(Byte value)
    {
        mode = value & 1;
        UpdateBanks();
    }

    private void SetROMBankNr(Byte value)
    {
        ROMBankNumber = value & 0x1F;
        UpdateBanks();
    }

    private void SetRAMBankNr(Byte value)
    {
        RAMBankNumber = value & 3;
        UpdateBanks();
    }

    private void UpdateBanks()
    {
        if (mode == 1)
        {
            ROMBankN.Switch(CorrectedROMBankNumber(ROMBankNumber));
            if (RAMBankN != null)
                RAMBankN.Switch(RAMBankNumber);
        }
        else
        {
            if (RAMBankN != null)
                RAMBankN.Switch(0);
            Byte concattedNr = (RAMBankNumber << 5) | ROMBankNumber;
            ROMBankN.Switch(CorrectedROMBankNumber(concattedNr));
        }
    }

    private Byte CorrectedROMBankNumber(Byte value)
    {
        if (ROMBankNumber > 0x60) value -= 4;
        else if (ROMBankNumber > 0x40) value -= 3;
        else if (ROMBankNumber > 0x20) value -= 2;
        else if (ROMBankNumber > 0) value -= 1;
        return value;
    }
}

class MBC2 : IMemory
{

    private IMemory ROMBank0;
    private Bank ROMBankN;
    private IMemory RAM;

    private const ushort RAMSize = 0x200;
    private bool isRamEnabled = false;

    private Address BankNStart = 0x4000;
    private Address BankNEnd = 0x8000;
    private Address RAMStart = 0xA000;
    private Address RAMEnd = 0xC000;

    private const ushort ROMSizePerBank = 0x4000;

    public MBC2(byte ROMBanks, byte[] cartridgeData)
    {
        if (ROMBanks > 15) throw new ArgumentException();

        ROMBank0 = new Memory(Cartridge.GetCartridgeChunk(0, BankNStart, cartridgeData), true);

        IMemory[] switchableBanks = new IMemory[ROMBanks];
        for (int i = 0; i < ROMBanks; i++)
        {
            int startAddress = ROMSizePerBank * (i + 1);
            switchableBanks[i] = new Memory(Cartridge.GetCartridgeChunk(startAddress, ROMSizePerBank, cartridgeData), true);
        }
        ROMBankN = new Bank(switchableBanks);

        RAM = new Memory(RAMSize);
    }


    public bool Read(Address address, out Byte value)
    {
        if (address < BankNStart)
            return ROMBank0.Read(address, out value);
        else if (address < BankNEnd)
            return ROMBankN.Read(address - BankNStart, out value);
        else if (address >= RAMStart && address < RAMEnd)
            return RAM.Read(address - RAMStart, out value);
        else throw new ArgumentOutOfRangeException();
    }

    private Address ToggleRAMEnd = 0x2000;
    private Address SwitchROMEnd = 0x4000;

    public bool Write(Address address, Byte value)
    {
        if (address < ToggleRAMEnd)
        {
            ToggleRAM(address);
        }
        else if (address < SwitchROMEnd)
        {
            SetROMBankNr(address, value);
        }
        else if (address >= RAMStart && address < RAMEnd)
        {
            if (isRamEnabled)
                return RAM.Write(address - RAMStart, value);
            return false;
        }
        else return false;

        return true;
    }

    private void ToggleRAM(Address address)
    {
        byte highByte = GetHighByte(address);
        if (TestBit(0, highByte))
            isRamEnabled = !isRamEnabled;
    }

    private void SetROMBankNr(Address address, Byte value)
    {
        Byte highByte = GetHighByte(address);
        if (TestBit(0, highByte))
        {
            ROMBankN.Switch(value & 0x0F);
        }
    }

}


class MBC3 : IMemory
{
    //TODO: implement internal RTC-clock

    private IMemory ROMBank0;
    private Bank ROMBankN;
    private Bank RAMAndRTCBankN;

    private const ushort RAMSize = 0x200;
    private bool isRamEnabled = false;

    private Address BankNStart = 0x4000;
    private Address BankNEnd = 0x8000;
    private Address RAMStart = 0xA000;
    private Address RAMEnd = 0xC000;

    private const ushort ROMSizePerBank = 0x4000;
    private const byte RTCRegisters = 5;

    public MBC3(bool hasRAM, byte ROMBanks, ushort RAMSize, byte[] cartridgeData)
    {
        ROMBank0 = new Memory(Cartridge.GetCartridgeChunk(0, ROMSizePerBank, cartridgeData), true);

        IMemory[] switchableBanks = new IMemory[ROMBanks];
        for (int i = 0; i < ROMBanks; i++)
        {
            int startAddress = ROMSizePerBank * (i + 1);
            switchableBanks[i] = new Memory(Cartridge.GetCartridgeChunk(startAddress, ROMSizePerBank, cartridgeData), true);
        }
        ROMBankN = new Bank(switchableBanks);


        byte count;
        ushort size;
        if (hasRAM)
        {
            count = 0;
            size = 0;
        }
        else
        {
            if (RAMSize == 0x8000)
            {
                count = 4;
                size = 0x2000;
            }
            else
            {
                count = 1;
                size = RAMSize;
            }
        }
        IMemory[] ramAndClock = new IMemory[0xC];
        for (int i = 0; i < 0xC; i++)
        {
            if (i > 3)
            {
                ramAndClock[i] = new Register();
            }
            else
            {
                if (i <= count)
                    ramAndClock[i] = new Memory(size);
                else
                    ramAndClock[i] = null;
            }
        }
        RAMAndRTCBankN = new Bank(ramAndClock);

    }

    public bool Read(Address address, out Byte value)
    {
        if (address < BankNStart)
            return ROMBank0.Read(address, out value);
        else if (address < BankNEnd)
            return ROMBankN.Read(address - BankNStart, out value);
        else if (address >= RAMStart && address < RAMEnd)
            return RAMAndRTCBankN.Read(address - RAMStart, out value);
        else throw new ArgumentOutOfRangeException();
    }


    private Address ToggleRAMEnd = 0x2000;
    private Address SwitchROMEnd = 0x4000;
    private Address SwitchRAMEnd = 0x6000;
    public bool Write(Address address, Byte value)
    {
        if (address < ToggleRAMEnd)
        {
            ToggleRAM(value);
        }
        else if (address < SwitchROMEnd)
        {
            SetROMBankNr(value);
        }
        else if (address < SwitchRAMEnd)
        {
            SetRAMBankNr(value);
        }
        else if (address >= RAMStart && address < RAMEnd)
        {
            if (isRamEnabled)
                return RAMAndRTCBankN.Write(address - RAMStart, value);
            return false;
        }
        else return false;

        return true;
    }

    private void ToggleRAM(Byte value)
    {
        isRamEnabled = value == 0x0A;
    }
    private void SetROMBankNr(Byte value)
    {
        byte bankNr = value & 0x7F;
        if (bankNr > 0)
            bankNr--;
        ROMBankN.Switch(bankNr);
    }

    private void SetRAMBankNr(Byte value)
    {
        value &= 0x0F;
        if (value > 0x0C) throw new ArgumentOutOfRangeException();
        RAMAndRTCBankN.Switch(value);
    }


}

class HuC1 : IMemory
{
    public bool Read(Address address, out Byte value)
    {
        throw new NotImplementedException();
    }

    public bool Write(Address address, Byte value)
    {
        throw new NotImplementedException();
    }
}
