using System.IO;
using Gameboi;
using Gameboi.Memory;
using static Gameboi.Statics.GeneralMemoryMap;

namespace Gameboi.Cartridges
{
    public abstract class Mbc : Cartridge
    {
        protected Mbc(string romPath) : base(romPath)
        {
        }

        protected abstract void OnBank0Write(Address address, Byte value);
        protected abstract void OnBank1Write(Address address, Byte value);

        public override void Connect(Bus bus)
        {
            bus.RouteMemory(ROM_bank_0_StartAddress, romBanks.GetBank(0), OnBank0Write);
            bus.RouteMemory(ROM_bank_n_StartAddress, romBanks, OnBank1Write);
            bus.RouteMemory(ExtRAM_StartAddress, ramBanks, ExtRAM_EndAddress);
        }
    }

    public class MbcRam : Bank
    {
        public bool isEnabled = false;

        public MbcRam(byte count, ushort size, string saveFileName = null) : base(count, size) => PrepareSaveFile(saveFileName);
        public MbcRam(IMemoryRange[] banks, string saveFileName = null) : base(banks) => PrepareSaveFile(saveFileName);

        private FileStream file;

        public void PrepareSaveFile(string saveFileName)
        {
            if (string.IsNullOrEmpty(saveFileName)) return;

            if (File.Exists(saveFileName))
            {
                byte[] allBytes = File.ReadAllBytes(saveFileName);
                pointer = 0;
                bool first = true;
                for (int i = 0; i < allBytes.Length; i++)
                {
                    while (Size == 0)
                        pointer++;
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

        public void CloseFileStream() => file?.Close();

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
}
