using Gameboi.Memory;

namespace Gameboi.Sound
{
    public class NR51 : Register
    {
        public bool Is1Out1 => data[0];
        public bool Is2Out1 => data[1];
        public bool Is3Out1 => data[2];
        public bool Is4Out1 => data[3];
        public bool Is1Out2 => data[4];
        public bool Is2Out2 => data[5];
        public bool Is3Out2 => data[6];
        public bool Is4Out2 => data[7];
    }
}
