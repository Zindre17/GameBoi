using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.SoundRegisters;

namespace GB_Emulator.Sound.channels
{
    public class Channel1 : SquareWaveChannel
    {
        public Channel1(NR52 _nr52) : base(_nr52, 0, true)
        {
            sweep.Write(0x80);
            waveDuty.Write(0x3F);
            envelope.Write(0x00);
            frequencyLow.Write(0xFF);
            frequencyHigh.Write(0xBF);
        }

        public override void Connect(Bus bus)
        {
            base.Connect(bus);

            bus.ReplaceMemory(NR10_address, sweep);
            bus.ReplaceMemory(NR11_address, waveDuty);
            bus.ReplaceMemory(NR12_address, envelope);
            bus.ReplaceMemory(NR13_address, frequencyLow);
            bus.ReplaceMemory(NR14_address, frequencyHigh);
            bus.RouteMemory(NR14_address + 1, new Dummy());
        }
    }
}