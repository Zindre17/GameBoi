
namespace Gameboi.Memory.Specials;

public class P1 : Register
{
    public P1() => data = 0xFF;

    public override void Write(Byte value) => data = value | data & 0xCF;

    public void SetActive(Byte value) => data = value | data & 0xF0;

    public Byte Active => data & 0x0F;

    public bool P15 => !data[5];
    public bool P14 => !data[4];

}

