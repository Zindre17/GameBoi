using System.Collections.Generic;
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

    public bool HasRamAndBattery => rom[0x147] is 3 or 6 or 9 or 0x10 or 0x13 or 0x1b or 0x1e;

    public bool HasRumble => rom[0x147] is 0x1c or 0x1d or 0x1e;

    public string GetTitle()
    {
        var titleBytes = new List<byte>();
        for (byte i = 0; i < titleLength; i++)
        {
            var value = rom[i + titleStart];
            if (value is 0)
            {
                break;
            }
            titleBytes.Add(value);
        }
        return Encoding.ASCII.GetString(titleBytes.ToArray());
    }

    private const ushort titleStart = 0x134;
    private const ushort titleEnd = 0x13f;
    private const byte titleLength = titleEnd + 1 - titleStart;

    private const ushort colorModeAddress = 0x143; // 0x80 both | 0xC0 only GBC | else only GB
}
