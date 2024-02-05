namespace Gameboi.Tools;

internal enum ArgumentType
{
    Address,
    Byte,
    SignedByte,
    None
}

internal interface IArgument
{
    int Value { get; }
}

internal record Argument(int Value) : IArgument;
internal record NoneArgument : IArgument
{
    private NoneArgument() { }
    public static NoneArgument Instance { get; } = new();

    public int Value => throw new InvalidOperationException($"{nameof(NoneArgument)} has no {nameof(Value)}");
}


internal class AssemblyConverter
{
    public static ArgumentType GetInstructionArgumentType(int opCode) => opCode switch
    {
        > 0xff => throw new ArgumentOutOfRangeException(nameof(opCode)),

        0x01 or 0x08 or
        0x11 or
        0x21 or
        0x31 or
        0xc2 or 0xc3 or 0xc4 or 0xca or 0xcc or 0xcd or
        0xd2 or 0xd4 or 0xda or 0xdc or
        0xea or
        0xfa => ArgumentType.Address,

        0x06 or 0x0e or
        0x10 or 0x16 or 0x1e or
        0x26 or 0x2e or
        0x36 or 0x3e or
        0xc6 or 0xce or
        0xd6 or 0xde or
        0xe0 or 0xe6 or 0xee or
        0xf0 or 0xf6 or 0xfe => ArgumentType.Byte,

        0x18 or 0x20 or 0x28 or 0x30 or 0x38 or 0xe8 or 0xf8 => ArgumentType.SignedByte,

        _ => ArgumentType.None
    };

    public static string GetMemoryLocationString(int address)
    {
        return address switch
        {
            < 0x8000 => "ROM",
            < 0xA000 => "Video RAM",
            < 0xC000 => "RAM",
            < 0xD000 => "Work RAM",
            < 0xE000 => "Work RAM",
            < 0xF000 => "Work RAM",
            < 0xFE00 => "Work RAM",
            < 0xFEA0 => "OAM",
            < 0xFF00 => "Unused",
            0xFF00 => "P1",
            0xFF01 => "SB",
            0xFF02 => "SC",
            0xFF04 => "DIV",
            0xFF05 => "TIMA",
            0xFF06 => "TMA",
            0xFF07 => "TAC",
            0xFF0F => "IF",
            0xFF10 => "NR10",
            0xFF11 => "NR11",
            0xFF12 => "NR12",
            0xFF13 => "NR13",
            0xFF14 => "NR14",
            0xFF16 => "NR21",
            0xFF17 => "NR22",
            0xFF18 => "NR23",
            0xFF19 => "NR24",
            0xFF1A => "NR30",
            0xFF1B => "NR31",
            0xFF1C => "NR32",
            0xFF1D => "NR33",
            0xFF1E => "NR34",
            0xFF20 => "NR41",
            0xFF21 => "NR42",
            0xFF22 => "NR43",
            0xFF23 => "NR44",
            0xFF24 => "NR50",
            0xFF25 => "NR51",
            0xFF26 => "NR52",
            >= 0xFF30 and < 0xFF40 => "Wave Pattern RAM",
            0xFF40 => "LCDC",
            0xFF41 => "STAT",
            0xFF42 => "SCY",
            0xFF43 => "SCX",
            0xFF44 => "LY",
            0xFF45 => "LYC",
            0xFF46 => "DMA",
            0xFF47 => "BGP",
            0xFF48 => "OBP0",
            0xFF49 => "OBP1",
            0xFF4A => "WY",
            0xFF4B => "WX",
            0xFF4d => "KEY1",
            0xFF4f => "VBK",
            0xFF51 => "HDMA1",
            0xFF52 => "HDMA2",
            0xFF53 => "HDMA3",
            0xFF54 => "HDMA4",
            0xFF55 => "HDMA5",
            0xFF68 => "BCPS",
            0xFF69 => "BCPD",
            0xFF6a => "OCPS",
            0xFF6b => "OCPD",
            0xFF70 => "SVBK",
            < 0xFF80 => "I/O unused",
            < 0xFFFF => "High RAM",
            _ => "Interrupt Enable Register",
        };
    }

    public static string ToString(int opCode, IArgument argument)
    {
        if (opCode > 0xff) throw new ArgumentOutOfRangeException(nameof(opCode));

        const string defaultD16 = "d16";
        const string defaultD8 = "d8";
        const string defaultS8 = "s8";

        var d16 = defaultD16;
        var d8 = defaultD8;
        var s8 = defaultS8;

        if (argument is Argument arg)
        {
            d16 = $"0x{arg.Value:X4} ({GetMemoryLocationString(arg.Value)})";
            d8 = $"0x{arg.Value:X2}";
            s8 = $"{arg.Value}";
        }

        switch (opCode)
        {
            case 0x00:
                return "NOP";
            case 0x01:
                return $"BC = {d16}";
            case 0x02:
                return "[BC] = A";
            case 0x03:
                return "BC++";
            case 0x04:
                return "B++";
            case 0x05:
                return "B--";
            case 0x06:
                return $"B = {d8}";
            case 0x07:
                return "A <<= 1 (wrap-around)";
            case 0x08:
                return $"[{d16}] = SP";
            case 0x09:
                return "HL = HL + BC";
            case 0x0A:
                return "A = [BC]";
            case 0x0B:
                return "BC--";
            case 0x0C:
                return "C++";
            case 0x0D:
                return "C--";
            case 0x0E:
                return $"C = {d8}";
            case 0x0F:
                return "A >>= 1 (wrap-around)";


            case 0x10:
                return $"Stop {d8}";
            case 0x11:
                return $"DE = {d16}";
            case 0x12:
                return "[DE] = A";
            case 0x13:
                return "DE++";
            case 0x14:
                return "D++";
            case 0x15:
                return "D--";
            case 0x16:
                return $"D = {d8}";
            case 0x17:
                return "A <<= 1";
            case 0x18:
                return $"PC += {s8}";
            case 0x19:
                return "HL += DE";
            case 0x1A:
                return "A = [DE]";
            case 0x1B:
                return "DE--";
            case 0x1C:
                return "E++";
            case 0x1D:
                return "E--";
            case 0x1E:
                return $"E = {d8}";
            case 0x1F:
                return "A >>= 1";


            case 0x20:
                return $"if( not zero ) PC += {s8} ";
            case 0x21:
                return $"HL = {d16}";
            case 0x22:
                return "[HL++] = A";
            case 0x23:
                return "HL++";
            case 0x24:
                return "H++";
            case 0x25:
                return "H--";
            case 0x26:
                return $"H = {d8}";
            case 0x27:
                return "DAA";
            case 0x28:
                return $"if( zero ) PC += {s8} ";
            case 0x29:
                return "HL += HL";
            case 0x2A:
                return "A = [HL++]";
            case 0x2B:
                return "HL--";
            case 0x2C:
                return "L++";
            case 0x2D:
                return "L--";
            case 0x2E:
                return $"L = {d8}";
            case 0x2F:
                return "Complement A";


            case 0x30:
                return $"if( not carry ) PC += {s8} ";
            case 0x31:
                return $"SP = {d16}";
            case 0x32:
                return "[HL--] = A";
            case 0x33:
                return "SP++";
            case 0x34:
                return "[HL]++";
            case 0x35:
                return "[HL]--";
            case 0x36:
                return $"[HL] = {d8}";
            case 0x37:
                return "Set carry flag";
            case 0x38:
                return $"if( carry ) PC += {s8} ";
            case 0x39:
                return "HL += SP";
            case 0x3A:
                return "A = [HL--]";
            case 0x3B:
                return "SP--";
            case 0x3C:
                return "A++";
            case 0x3D:
                return "A--";
            case 0x3E:
                return $"A = {d8}";
            case 0x3F:
                return "Flip carry flag";


            case 0x40:
                return "B = B";
            case 0x41:
                return "B = C";
            case 0x42:
                return "B = D";
            case 0x43:
                return "B = E";
            case 0x44:
                return "B = H";
            case 0x45:
                return "B = L";
            case 0x46:
                return "B = [HL]";
            case 0x47:
                return "B = A";
            case 0x48:
                return "C = B";
            case 0x49:
                return "C = C";
            case 0x4A:
                return "C = D";
            case 0x4B:
                return "C = E";
            case 0x4C:
                return "C = H";
            case 0x4D:
                return "C = L";
            case 0x4E:
                return "C = [HL]";
            case 0x4F:
                return "C = A";


            case 0x50:
                return "D = B";
            case 0x51:
                return "D = C";
            case 0x52:
                return "D = D";
            case 0x53:
                return "D = E";
            case 0x54:
                return "D = H";
            case 0x55:
                return "D = L";
            case 0x56:
                return "D = [HL]";
            case 0x57:
                return "D = A";
            case 0x58:
                return "E = B";
            case 0x59:
                return "E = C";
            case 0x5A:
                return "E = D";
            case 0x5B:
                return "E = E";
            case 0x5C:
                return "E = H";
            case 0x5D:
                return "E = L";
            case 0x5E:
                return "E = [HL]";
            case 0x5F:
                return "E = A";


            case 0x60:
                return "H = B";
            case 0x61:
                return "H = C";
            case 0x62:
                return "H = D";
            case 0x63:
                return "H = E";
            case 0x64:
                return "H = H";
            case 0x65:
                return "H = L";
            case 0x66:
                return "H = [HL]";
            case 0x67:
                return "H = A";
            case 0x68:
                return "L = B";
            case 0x69:
                return "L = C";
            case 0x6A:
                return "L = D";
            case 0x6B:
                return "L = E";
            case 0x6C:
                return "L = H";
            case 0x6D:
                return "L = L";
            case 0x6E:
                return "L = [HL]";
            case 0x6F:
                return "L = A";



            case 0x70:
                return "[HL] = B";
            case 0x71:
                return "[HL] = C";
            case 0x72:
                return "[HL] = D";
            case 0x73:
                return "[HL] = E";
            case 0x74:
                return "[HL] = H";
            case 0x75:
                return "[HL] = L";
            case 0x76:
                return "Halt";
            case 0x77:
                return "[HL] = A";
            case 0x78:
                return "A = B";
            case 0x79:
                return "A = C";
            case 0x7A:
                return "A = D";
            case 0x7B:
                return "A = E";
            case 0x7C:
                return "A = H";
            case 0x7D:
                return "A = L";
            case 0x7E:
                return "A = [HL]";
            case 0x7F:
                return "A = A";


            case 0x80:
                return "A += B";
            case 0x81:
                return "A += C";
            case 0x82:
                return "A += D";
            case 0x83:
                return "A += E";
            case 0x84:
                return "A += H";
            case 0x85:
                return "A += L";
            case 0x86:
                return "A += [HL]";
            case 0x87:
                return "A += A";
            case 0x88:
                return "A += B with carry";
            case 0x89:
                return "A += C with carry";
            case 0x8A:
                return "A += D with carry";
            case 0x8B:
                return "A += E with carry";
            case 0x8C:
                return "A += H with carry";
            case 0x8D:
                return "A += L with carry";
            case 0x8E:
                return "A += [HL] with carry";
            case 0x8F:
                return "A += A with carry";


            case 0x90:
                return "A -= B";
            case 0x91:
                return "A -= C";
            case 0x92:
                return "A -= D";
            case 0x93:
                return "A -= E";
            case 0x94:
                return "A -= H";
            case 0x95:
                return "A -= L";
            case 0x96:
                return "A -= [HL]";
            case 0x97:
                return "A -= A";
            case 0x98:
                return "A -= B with carry";
            case 0x99:
                return "A -= C with carry";
            case 0x9A:
                return "A -= D with carry";
            case 0x9B:
                return "A -= E with carry";
            case 0x9C:
                return "A -= H with carry";
            case 0x9D:
                return "A -= L with carry";
            case 0x9E:
                return "A -= [HL] with carry";
            case 0x9F:
                return "A -= A with carry";


            case 0xA0:
                return "A &= B";
            case 0xA1:
                return "A &= C";
            case 0xA2:
                return "A &= D";
            case 0xA3:
                return "A &= E";
            case 0xA4:
                return "A &= H";
            case 0xA5:
                return "A &= L";
            case 0xA6:
                return "A &= [HL]";
            case 0xA7:
                return "A &= A";
            case 0xA8:
                return "A ^= B";
            case 0xA9:
                return "A ^= C";
            case 0xAA:
                return "A ^= D";
            case 0xAB:
                return "A ^= E";
            case 0xAC:
                return "A ^= H";
            case 0xAD:
                return "A ^= L";
            case 0xAE:
                return "A ^= [HL]";
            case 0xAF:
                return "A ^= A";


            case 0xB0:
                return "A |= B";
            case 0xB1:
                return "A |= C";
            case 0xB2:
                return "A |= D";
            case 0xB3:
                return "A |= E";
            case 0xB4:
                return "A |= H";
            case 0xB5:
                return "A |= L";
            case 0xB6:
                return "A |= [HL]";
            case 0xB7:
                return "A |= A";
            case 0xB8:
                return "A == B";
            case 0xB9:
                return "A == C";
            case 0xBA:
                return "A == D";
            case 0xBB:
                return "A == E";
            case 0xBC:
                return "A == H";
            case 0xBD:
                return "A == L";
            case 0xBE:
                return "A == [HL]";
            case 0xBF:
                return "A == A";


            case 0xC0:
                return "if( not zero ) return";
            case 0xC1:
                return "BC = Pop(SP)";
            case 0xC2:
                return $"if( not zero ) PC = {d16}";
            case 0xC3:
                return $"PC = {d16}";
            case 0xC4:
                return $"if( not zero ) Call {d16}";
            case 0xC5:
                return "Push BC";
            case 0xC6:
                return $"A += {d8}";
            case 0xC7:
                return "Call 0x00 (restart)";
            case 0xC8:
                return "if( zero ) return";
            case 0xC9:
                return "return";
            case 0xCA:
                return $"if( zero ) PC = {d16}";
            case 0xCB:
                return "Prefix CB (bit operations)";
            case 0xCC:
                return $"if( zero ) Call {d16}";
            case 0xCD:
                return $"Call {d16}";
            case 0xCE:
                return $"A += {d8} with carry";
            case 0xCF:
                return "Call 0x08 (restart)";


            case 0xD0:
                return "if( not carry ) return";
            case 0xD1:
                return "DE = Pop(SP)";
            case 0xD2:
                return $"if( not carry ) PC = {d16}";
            case 0xD3:
                return "Illegal instruction";
            case 0xD4:
                return $"if( not carry ) Call {d16}";
            case 0xD5:
                return "Push DE";
            case 0xD6:
                return $"A -= {d8}";
            case 0xD7:
                return "Call 0x10 (restart)";
            case 0xD8:
                return "if( carry ) return";
            case 0xD9:
                return "return and enable interrupt";
            case 0xDA:
                return $"if( carry ) PC = {d16}";
            case 0xDB:
                return "Illegal instruction";
            case 0xDC:
                return $"if( carry ) Call {d16}";
            case 0xDD:
                return "Illegal instruction";
            case 0xDE:
                return $"A -= {d8} with carry";
            case 0xDF:
                return "Call 0x18 (restart)";


            case 0xE0:
                return $"[0xFF00 + {d8} ({GetMemoryLocationString(0xff00 + argument.Value)})] = A";
            case 0xE1:
                return "HL = Pop(SP)";
            case 0xE2:
                return "[0xFF00 + C] = A";
            case 0xE3:
                return "Illegal instruction";
            case 0xE4:
                return "Illegal instruction";
            case 0xE5:
                return "Push HL";
            case 0xE6:
                return $"A &= {d8}";
            case 0xE7:
                return "Call 0x20 (restart)";
            case 0xE8:
                return $"SP += {s8}";
            case 0xE9:
                return "PC = HL";
            case 0xEA:
                return $"[{d16}] = A";
            case 0xEB:
                return "Illegal instruction";
            case 0xEC:
                return "Illegal instruction";
            case 0xED:
                return "Illegal instruction";
            case 0xEE:
                return $"A ^= {d8}";
            case 0xEF:
                return "Call 0x28 (restart)";


            case 0xF0:
                return $"A = [0xFF00 + {d8} ({GetMemoryLocationString(0xff00 + argument.Value)})]";
            case 0xF1:
                return "AF = Pop(SP)";
            case 0xF2:
                return "A = [0xFF00 + C]";
            case 0xF3:
                return "Disable interrupt";
            case 0xF4:
                return "Illegal instruction";
            case 0xF5:
                return "Push AF";
            case 0xF6:
                return $"A |= {d8}";
            case 0xF7:
                return "Call 0x30 (restart)";
            case 0xF8:
                return $"HL = SP + {s8}";
            case 0xF9:
                return "SP = HL";
            case 0xFA:
                return $"A = [{d16}]";
            case 0xFB:
                return "Enable interrupt";
            case 0xFC:
                return "Illegal instruction";
            case 0xFD:
                return "Illegal instruction";
            case 0xFE:
                return $"A == {d8}";
            case 0xFF:
                return "Call 0x38 (restart)";
            default: throw new ArgumentOutOfRangeException(nameof(opCode));
        }
    }
}
