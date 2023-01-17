using Gameboi.Memory;

namespace Gameboi.Sound
{
    public class NR50 : Register
    {
        public bool IsVinOut1 => data[3];
        public bool IsVinOut2 => data[7];

        public Byte VolumeOut2 => (data & 0x70) >> 4;
        public Byte VolumeOut1 => data & 7;
    }
}
