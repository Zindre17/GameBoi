using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Sound
{
    public class NR50 : Register
    {
        public bool IsVinOut1 => data[3];
        public bool IsVinOut2 => data[7];

        private Byte VolumeOut2 => (data & 0x70) >> 4;
        private Byte VolumeOut1 => data & 7;

        public double GetVolumeScaler(bool out1)
        {
            Byte data;
            if (out1)
                data = VolumeOut1;
            else
                data = VolumeOut2;
            return data / 7d;
        }
    }
}