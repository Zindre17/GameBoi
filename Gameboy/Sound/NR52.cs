using GB_Emulator;
using GB_Emulator.Memory;

namespace GB_Emulator.Sound
{
    public class NR52 : MaskedRegister
    {
        private readonly Bus bus;
        public NR52(Bus bus) : base(0x70)
        {
            this.bus = bus;
        }

        public bool IsSoundOn => data[7];

        public bool IsChannelOn(int channel)
        {
            if (channel > 3) throw new System.Exception();
            return data[channel];
        }

        public override void Write(Byte value)
        {
            if (!value[7])
            {
                base.Write(0);
                ResetAllSoundRegs();
                bus.SetCpuWriteFilter(BlockWritesWhenOff);
            }
            else
            {
                bus.ClearCpuWriteFilter();
                base.Write(0x80 | data);
            }
        }

        private bool BlockWritesWhenOff(Address address)
        {
            return address < Statics.SoundRegisters.NR10_address || Statics.SoundRegisters.NR52_address <= address;
        }

        private void ResetAllSoundRegs()
        {
            for (int i = Statics.SoundRegisters.NR10_address; i < Statics.SoundRegisters.NR52_address; i++)
            {
                bus.Write(i, 0);
            }
        }

        public void TurnAllOn() => data |= 0x80;

        public void TurnAllOff() => data &= 0x70;

        public void TurnOn(int channel) => data |= 1 << channel;
        public void TurnOff(int channel) => data &= ~(1 << channel);

    }
}
