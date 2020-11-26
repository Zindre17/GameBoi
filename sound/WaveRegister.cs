public class WaveRegister : Register
{
    public Byte Second => data & 0x0F;
    public Byte First => (data & 0xF0) >> 4;
}