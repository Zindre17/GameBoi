using System;
using System.IO;

abstract class Mbc : Cartridge
{
    protected Mbc(string romPath) : base(romPath)
    {
    }

    protected abstract void OnBank0Write(Address address, Byte value);
    protected abstract void OnBank1Write(Address address, Byte value);
}

class MbcRom : MemoryRange
{

    public MbcRom(Byte[] memory, Action<Address, Byte> onWrite) : base(memory, true)
    {
        OnWrite = onWrite;
    }

    public MbcRom(IMemory[] memory, Action<Address, Byte> onWrite) : base(memory)
    {
        OnWrite = onWrite;
    }

    public Action<Address, Byte> OnWrite;

    public override void Write(Address address, Byte value, bool isCpu = false)
    {
        OnWrite?.Invoke(address, value);
    }

}

class MbcRam : Bank
{
    public bool isEnabled = false;

    public MbcRam(byte count, ushort size, string saveFileName = null) : base(count, size) => PrepareSaveFile(saveFileName);
    public MbcRam(IMemoryRange[] banks, string saveFileName = null) : base(banks) => PrepareSaveFile(saveFileName);

    private FileStream file;

    private void PrepareSaveFile(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName)) return;

        if (File.Exists(saveFileName))
        {
            byte[] allBytes = File.ReadAllBytes(saveFileName);
            pointer = 0;
            bool first = true;
            for (int i = 0; i < allBytes.Length; i++)
            {
                Address relAdr = i % Size;
                if (relAdr == 0 && !first) pointer++;
                else first = false;
                base.Write(relAdr, allBytes[i]);
            }
        }
        else
        {
            byte[] bytes = new byte[GetTotalSize()];
            File.WriteAllBytes(saveFileName, bytes);
        }

        file = File.OpenWrite(saveFileName);
    }
    public void CloseFileStream() => file.Close();

    public override Byte Read(Address address, bool isCpu = false)
    {
        if (isEnabled) return base.Read(address, isCpu);
        return 0xFF;
    }
    public override void Write(Address address, Byte value, bool isCpu = false)
    {
        if (isEnabled)
        {
            base.Write(address, value, isCpu);
            int offset = 0;
            for (int i = 0; i < pointer; i++)
                offset += banks[i].Size;
            file.Position = offset + address;
            file.WriteByte(value);
        }
    }
}