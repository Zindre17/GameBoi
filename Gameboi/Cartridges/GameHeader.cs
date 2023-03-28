using System.Text;
namespace Gameboi.Cartridges;

public class GameHeader
{
    private readonly byte[] rom;

    public GameHeader(byte[] rom)
    {
        this.rom = rom;
    }

    public bool IsColorGame => rom[colorModeAddress] is 0x80 or 0xc0;

    public string GetTitle()
    {
        byte[] titleBytes = new byte[titleLength];
        for (byte i = 0; i < titleLength; i++)
        {
            titleBytes[i] = rom[i + titleStart];
        }
        return Encoding.ASCII.GetString(titleBytes, 0, titleLength);
    }

    private const ushort titleStart = 0x134;
    private const ushort titleEnd = 0x143;
    private const byte titleLength = titleEnd + 1 - titleStart;

    private const ushort colorModeAddress = 0x143; // 0x80 both | 0xC0 only GBC | else only GB
}
