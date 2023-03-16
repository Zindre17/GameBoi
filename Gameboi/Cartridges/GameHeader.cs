using System;
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

    public MbcType GetCartridgeType()
    {
        return rom[cartridgeTypeAddress] switch
        {
            0 or 8 or 9 => MbcType.NoMbc,
            1 or 2 or 3 => MbcType.Mbc1,
            5 or 6 => MbcType.Mbc2,
            >= 0xF and <= 0x13 => MbcType.Mbc3,
            >= 0x19 and <= 0x1E => MbcType.Mbc5,
            _ => throw new Exception("Does not support cartridge type")
        };
    }

    private const int cartridgeTypeAddress = 0x147;

    private const ushort titleStart = 0x134;
    private const ushort titleEnd = 0x143;
    private const byte titleLength = titleEnd + 1 - titleStart;

    private const ushort colorModeAddress = 0x143; // 0x80 both | 0xC0 only GBC | else only GB
}
