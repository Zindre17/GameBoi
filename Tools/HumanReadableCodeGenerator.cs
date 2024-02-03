using System.Text;

namespace Gameboi.Tools;

public class HumanReadableCodeGenerator
{
    public void InterpretRom(string filePath)
    {
        var allBytes = File.ReadAllBytes(filePath);
        var outputFile = File.OpenWrite(filePath + ".gbcode");

        var program = new string[allBytes.Length];

        var branches = new Queue<int>();

        int pc = 0;
        var visited = new bool[allBytes.Length];
        var isBranchStart = new bool[allBytes.Length];
        var isBranchEnd = new bool[allBytes.Length];
        var alwaysJumps = new bool[allBytes.Length];

        int GetDirectAddress()
        {
            var low = allBytes[pc + 1];
            var high = allBytes[pc + 2];
            return (high << 8) | low;
        }
        string GetFormattedDirectAddress()
        {
            return $"0x{GetDirectAddress():X4}";
        }

        int GetDirectByte()
        {
            return allBytes[pc + 1];
        }
        string GetFormattedDirectByte()
        {
            return $"0x{GetDirectByte():X2}";
        }

        sbyte jumpDistance;
        int jumpAddress;
        var branchComplete = true;

        void Visit(string message, int inputs = 0)
        {
            visited[pc] = true;
            program[pc++] = message;
            if (inputs > 0)
            {
                visited[pc] = true;
                program[pc++] = "Direct input";
            }
            if (inputs > 1)
            {
                visited[pc] = true;
                program[pc++] = "Direct input";
            }
        }

        void Branch(int address)
        {
            if (!visited[address])
            {
                branches.Enqueue(address);
                isBranchStart[address] = true;
            }
        }

        void EndBranch()
        {
            isBranchEnd[pc - 1] = true;
            branchComplete = true;
        }

        Branch(0);
        Branch(8);
        Branch(0x10);
        Branch(0x18);
        Branch(0x20);
        Branch(0x28);
        Branch(0x30);
        Branch(0x38);
        Branch(0x40);
        Branch(0x48);
        Branch(0x50);
        Branch(0x58);
        Branch(0x60);
        Branch(0x100);

        while (branches.Any() || branchComplete is false)
        {
            if (branchComplete || visited[pc])
            {
                if (!branches.Any())
                {
                    break;
                }
                pc = branches.Dequeue();
                Console.WriteLine($"Starting branch at 0x{pc:X4}");
                branchComplete = false;
            }
            switch (allBytes[pc])
            {
                case 0x00:
                    Visit("NOP");
                    break;
                case 0x01:
                    Visit($"Load {GetFormattedDirectAddress()} into BC", 2);
                    break;
                case 0x02:
                    Visit("Load A into memory address of BC");
                    break;
                case 0x03:
                    Visit("Increment BC");
                    break;
                case 0x04:
                    Visit("Increment B");
                    break;
                case 0x05:
                    Visit("Decrement B");
                    break;
                case 0x06:
                    Visit($"Load {GetFormattedDirectByte()} into B", 1);
                    break;
                case 0x07:
                    Visit("Bitshift A left with wrap-around");
                    break;
                case 0x08:
                    Visit($"Load stack pointer into direct memory address {GetFormattedDirectAddress()}", 2);
                    break;
                case 0x09:
                    Visit("Add BC into HL");
                    break;
                case 0x0A:
                    Visit("Load from memory address in BC into A");
                    break;
                case 0x0B:
                    Visit("Decrement BC");
                    break;
                case 0x0C:
                    Visit("Increment C");
                    break;
                case 0x0D:
                    Visit("Decrement C");
                    break;
                case 0x0E:
                    Visit($"Load {GetFormattedDirectByte()} into C", 1);
                    break;
                case 0x0F:
                    Visit($"Bitshift A right with wrap-around");
                    break;


                case 0x10:
                    Visit($"Stop {GetFormattedDirectByte()}", 1);
                    EndBranch();
                    break;
                case 0x11:
                    Visit($"Load {GetFormattedDirectAddress()} into DE", 2);
                    break;
                case 0x12:
                    Visit("Load A into memory address of DE");
                    break;
                case 0x13:
                    Visit("Increment DE");
                    break;
                case 0x14:
                    Visit("Increment D");
                    break;
                case 0x15:
                    Visit("Decrement D");
                    break;
                case 0x16:
                    Visit($"Load {GetFormattedDirectByte()} into D", 1);
                    break;
                case 0x17:
                    Visit("Bitshift A left");
                    break;
                case 0x18:
                    jumpDistance = (sbyte)GetDirectByte();
                    Visit($"Jump by {jumpDistance}", 1);
                    alwaysJumps[pc - 1] = true;
                    pc += jumpDistance;
                    break;
                case 0x19:
                    Visit("Add DE into HL");
                    break;
                case 0x1A:
                    Visit("Load from memory address in DE into A");
                    break;
                case 0x1B:
                    Visit("Decrement DE");
                    break;
                case 0x1C:
                    Visit("Increment E");
                    break;
                case 0x1D:
                    Visit("Decrement E");
                    break;
                case 0x1E:
                    Visit($"Load {GetFormattedDirectByte()} into E", 1);
                    break;
                case 0x1F:
                    Visit($"Bitshift A right");
                    break;


                case 0x20:
                    jumpDistance = (sbyte)GetDirectByte();
                    Visit($"Jump by {jumpDistance} if not zero flag is set", 1);
                    Branch(pc + jumpDistance);
                    break;
                case 0x21:
                    Visit($"Load {GetFormattedDirectAddress()} into HL", 2);
                    break;
                case 0x22:
                    Visit("Load A into memory address of HL and increment HL");
                    break;
                case 0x23:
                    Visit("Increment HL");
                    break;
                case 0x24:
                    Visit("Increment H");
                    break;
                case 0x25:
                    Visit("Decrement H");
                    break;
                case 0x26:
                    Visit($"Load {GetFormattedDirectByte()} into H", 1);
                    break;
                case 0x27:
                    Visit("DAA");
                    break;
                case 0x28:
                    jumpDistance = (sbyte)GetDirectByte();
                    Visit($"Jump by {jumpDistance} if zero flag is set", 1);
                    Branch(pc + jumpDistance);
                    break;
                case 0x29:
                    Visit("Add HL into HL");
                    break;
                case 0x2A:
                    Visit("Load from memory address in HL into A and increment HL");
                    break;
                case 0x2B:
                    Visit("Decrement HL");
                    break;
                case 0x2C:
                    Visit("Increment L");
                    break;
                case 0x2D:
                    Visit("Decrement L");
                    break;
                case 0x2E:
                    Visit($"Load {GetFormattedDirectByte()} into L", 1);
                    break;
                case 0x2F:
                    Visit("Complement A");
                    break;


                case 0x30:
                    jumpDistance = (sbyte)GetDirectByte();
                    Visit($"Jump by {jumpDistance} if not carry flag is set", 1);
                    Branch(pc + jumpDistance);
                    break;
                case 0x31:
                    Visit($"Load {GetFormattedDirectAddress()} into stack pointer", 2);
                    break;
                case 0x32:
                    Visit("Load A into memory address of HL and decrement HL", 2);
                    break;
                case 0x33:
                    Visit("Increment stack pointer");
                    break;
                case 0x34:
                    Visit("Increment in memory at address of HL");
                    break;
                case 0x35:
                    Visit("Decrement in memory at address of HL");
                    break;
                case 0x36:
                    Visit($"Load {GetFormattedDirectByte()} into memory address in HL", 1);
                    break;
                case 0x37:
                    Visit("Set carry flag");
                    break;
                case 0x38:
                    jumpDistance = (sbyte)GetDirectByte();
                    Visit($"Jump by {jumpDistance} if carry flag is set", 1);
                    Branch(pc + jumpDistance);
                    break;
                case 0x39:
                    Visit("Add SP into HL");
                    break;
                case 0x3A:
                    Visit("Load from memory address in HL into A and decrement HL");
                    break;
                case 0x3B:
                    Visit("Decrement SP");
                    break;
                case 0x3C:
                    Visit("Increment A");
                    break;
                case 0x3D:
                    Visit("Decrement A");
                    break;
                case 0x3E:
                    Visit($"Load {GetFormattedDirectByte()} into A", 1);
                    break;
                case 0x3F:
                    Visit("Flip carry flag");
                    break;


                case 0x40:
                    Visit("Load B into B");
                    break;
                case 0x41:
                    Visit("Load C into B");
                    break;
                case 0x42:
                    Visit("Load D into B");
                    break;
                case 0x43:
                    Visit("Load E into B");
                    break;
                case 0x44:
                    Visit("Load H into B");
                    break;
                case 0x45:
                    Visit("Load L into B");
                    break;
                case 0x46:
                    Visit("Load from memory address in HL into B");
                    break;
                case 0x47:
                    Visit("Load A into B");
                    break;
                case 0x48:
                    Visit("Load B into C");
                    break;
                case 0x49:
                    Visit("Load C into C");
                    break;
                case 0x4A:
                    Visit("Load D into C");
                    break;
                case 0x4B:
                    Visit("Load E into C");
                    break;
                case 0x4C:
                    Visit("Load H into C");
                    break;
                case 0x4D:
                    Visit("Load L into C");
                    break;
                case 0x4E:
                    Visit("Load from memory address in HL into C");
                    break;
                case 0x4F:
                    Visit("Load A into C");
                    break;


                case 0x50:
                    Visit("Load B into D");
                    break;
                case 0x51:
                    Visit("Load C into D");
                    break;
                case 0x52:
                    Visit("Load D into D");
                    break;
                case 0x53:
                    Visit("Load E into D");
                    break;
                case 0x54:
                    Visit("Load H into D");
                    break;
                case 0x55:
                    Visit("Load L into D");
                    break;
                case 0x56:
                    Visit("Load from memory address in HL into D");
                    break;
                case 0x57:
                    Visit("Load A into D");
                    break;
                case 0x58:
                    Visit("Load B into E");
                    break;
                case 0x59:
                    Visit("Load C into E");
                    break;
                case 0x5A:
                    Visit("Load D into E");
                    break;
                case 0x5B:
                    Visit("Load E into E");
                    break;
                case 0x5C:
                    Visit("Load H into E");
                    break;
                case 0x5D:
                    Visit("Load L into E");
                    break;
                case 0x5E:
                    Visit("Load from memory address in HL into E");
                    break;
                case 0x5F:
                    Visit("Load A into E");
                    break;


                case 0x60:
                    Visit("Load B into H");
                    break;
                case 0x61:
                    Visit("Load C into H");
                    break;
                case 0x62:
                    Visit("Load D into H");
                    break;
                case 0x63:
                    Visit("Load E into H");
                    break;
                case 0x64:
                    Visit("Load H into H");
                    break;
                case 0x65:
                    Visit("Load L into H");
                    break;
                case 0x66:
                    Visit("Load from memory address in HL into H");
                    break;
                case 0x67:
                    Visit("Load A into H");
                    break;
                case 0x68:
                    Visit("Load B into L");
                    break;
                case 0x69:
                    Visit("Load C into L");
                    break;
                case 0x6A:
                    Visit("Load D into L");
                    break;
                case 0x6B:
                    Visit("Load E into L");
                    break;
                case 0x6C:
                    Visit("Load H into L");
                    break;
                case 0x6D:
                    Visit("Load L into L");
                    break;
                case 0x6E:
                    Visit("Load from memory address in HL into L");
                    break;
                case 0x6F:
                    Visit("Load A into L");
                    break;


                case 0x70:
                    Visit("Load B into memory address of HL");
                    break;
                case 0x71:
                    Visit("Load C into memory address of HL");
                    break;
                case 0x72:
                    Visit("Load D into memory address of HL");
                    break;
                case 0x73:
                    Visit("Load E into memory address of HL");
                    break;
                case 0x74:
                    Visit("Load H into memory address of HL");
                    break;
                case 0x75:
                    Visit("Load L into memory address of HL");
                    break;
                case 0x76:
                    Visit("Halt");
                    EndBranch();
                    break;
                case 0x77:
                    Visit("Load A into memory address of HL");
                    break;
                case 0x78:
                    Visit("Load B into A");
                    break;
                case 0x79:
                    Visit("Load C into A");
                    break;
                case 0x7A:
                    Visit("Load D into A");
                    break;
                case 0x7B:
                    Visit("Load E into A");
                    break;
                case 0x7C:
                    Visit("Load H into A");
                    break;
                case 0x7D:
                    Visit("Load L into A");
                    break;
                case 0x7E:
                    Visit("Load from memory address in HL into A");
                    break;
                case 0x7F:
                    Visit("Load A into A");
                    break;


                case 0x80:
                    Visit("Add B into A");
                    break;
                case 0x81:
                    Visit("Add C into A");
                    break;
                case 0x82:
                    Visit("Add D into A");
                    break;
                case 0x83:
                    Visit("Add E into A");
                    break;
                case 0x84:
                    Visit("Add H into A");
                    break;
                case 0x85:
                    Visit("Add L into A");
                    break;
                case 0x86:
                    Visit("Add from memory address in HL into A");
                    break;
                case 0x87:
                    Visit("Add A into A");
                    break;
                case 0x88:
                    Visit("Add B into A with carry");
                    break;
                case 0x89:
                    Visit("Add C into A with carry");
                    break;
                case 0x8A:
                    Visit("Add D into A with carry");
                    break;
                case 0x8B:
                    Visit("Add E into A with carry");
                    break;
                case 0x8C:
                    Visit("Add H into A with carry");
                    break;
                case 0x8D:
                    Visit("Add L into A with carry");
                    break;
                case 0x8E:
                    Visit("Add from memory address in HL into A with carry");
                    break;
                case 0x8F:
                    Visit("Add A into A with carry");
                    break;


                case 0x90:
                    Visit("Subtract B from A");
                    break;
                case 0x91:
                    Visit("Subtract C from A");
                    break;
                case 0x92:
                    Visit("Subtract D from A");
                    break;
                case 0x93:
                    Visit("Subtract E from A");
                    break;
                case 0x94:
                    Visit("Subtract H from A");
                    break;
                case 0x95:
                    Visit("Subtract L from A");
                    break;
                case 0x96:
                    Visit("Subtract from memory address in HL into A");
                    break;
                case 0x97:
                    Visit("Subtract A from A");
                    break;
                case 0x98:
                    Visit("Subtract B from A with carry");
                    break;
                case 0x99:
                    Visit("Subtract C from A with carry");
                    break;
                case 0x9A:
                    Visit("Subtract D from A with carry");
                    break;
                case 0x9B:
                    Visit("Subtract E from A with carry");
                    break;
                case 0x9C:
                    Visit("Subtract H from A with carry");
                    break;
                case 0x9D:
                    Visit("Subtract L from A with carry");
                    break;
                case 0x9E:
                    Visit("Subtract from memory address in HL into A with carry");
                    break;
                case 0x9F:
                    Visit("Subtract A from A with carry");
                    break;


                case 0xA0:
                    Visit("AND B into A");
                    break;
                case 0xA1:
                    Visit("AND C into A");
                    break;
                case 0xA2:
                    Visit("AND D into A");
                    break;
                case 0xA3:
                    Visit("AND E into A");
                    break;
                case 0xA4:
                    Visit("AND H into A");
                    break;
                case 0xA5:
                    Visit("AND L into A");
                    break;
                case 0xA6:
                    Visit("AND from memory address in HL into A");
                    break;
                case 0xA7:
                    Visit("AND A into A");
                    break;
                case 0xA8:
                    Visit("XOR B into A");
                    break;
                case 0xA9:
                    Visit("XOR C into A");
                    break;
                case 0xAA:
                    Visit("XOR D into A");
                    break;
                case 0xAB:
                    Visit("XOR E into A");
                    break;
                case 0xAC:
                    Visit("XOR H into A");
                    break;
                case 0xAD:
                    Visit("XOR L into A");
                    break;
                case 0xAE:
                    Visit("XOR from memory address in HL into A");
                    break;
                case 0xAF:


                    Visit("XOR A into A");
                    break;
                case 0xB0:
                    Visit("OR B into A");
                    break;
                case 0xB1:
                    Visit("OR C into A");
                    break;
                case 0xB2:
                    Visit("OR D into A");
                    break;
                case 0xB3:
                    Visit("OR E into A");
                    break;
                case 0xB4:
                    Visit("OR H into A");
                    break;
                case 0xB5:
                    Visit("OR L into A");
                    break;
                case 0xB6:
                    Visit("OR from memory address in HL into A");
                    break;
                case 0xB7:
                    Visit("OR A into A");
                    break;
                case 0xB8:
                    Visit("Compare B and A");
                    break;
                case 0xB9:
                    Visit("Compare C and A");
                    break;
                case 0xBA:
                    Visit("Compare D and A");
                    break;
                case 0xBB:
                    Visit("Compare E and A");
                    break;
                case 0xBC:
                    Visit("Compare H and A");
                    break;
                case 0xBD:
                    Visit("Compare L and A");
                    break;
                case 0xBE:
                    Visit("Compare from memory address in HL and A");
                    break;
                case 0xBF:
                    Visit("Compare A and A");
                    break;


                case 0xC0:
                    Visit("Return if not zero");
                    break;
                case 0xC1:
                    Visit("Pop from stack into BC");
                    break;
                case 0xC2:
                    jumpAddress = GetDirectAddress();
                    Visit($"Jump to {GetFormattedDirectAddress()} if not zero", 2);
                    Branch(jumpAddress);
                    break;
                case 0xC3:
                    jumpAddress = GetDirectAddress();
                    Visit($"Jump to {GetFormattedDirectAddress()}", 2);
                    alwaysJumps[pc - 1] = true;
                    pc = jumpAddress;
                    break;
                case 0xC4:
                    jumpAddress = GetDirectAddress();
                    Visit($"Call {GetFormattedDirectAddress()} if not zero", 2);
                    Branch(jumpAddress);
                    break;
                case 0xC5:
                    Visit("Push BC to stack");
                    break;
                case 0xC6:
                    Visit($"Add {GetFormattedDirectByte()} into A", 1);
                    break;
                case 0xC7:
                    Visit("Restart at 0");
                    EndBranch();
                    break;
                case 0xC8:
                    Visit("Return if zero");
                    break;
                case 0xC9:
                    Visit("Return");
                    EndBranch();
                    break;
                case 0xCA:
                    jumpAddress = GetDirectAddress();
                    Visit($"Jump to {GetFormattedDirectAddress()} if zero", 2);
                    Branch(jumpAddress);
                    break;
                case 0xCB:
                    Visit("Prefix CB");
                    Visit("Prefix instruction");
                    break;
                case 0xCC:
                    jumpAddress = GetDirectAddress();
                    Visit($"Call {GetFormattedDirectAddress()} if zero", 2);
                    Branch(jumpAddress);
                    break;
                case 0xCD:
                    jumpAddress = GetDirectAddress();
                    Visit($"Call {GetFormattedDirectAddress()}", 2);
                    Branch(jumpAddress);
                    break;
                case 0xCE:
                    Visit($"Add {GetFormattedDirectByte()} into A with carry", 1);
                    break;
                case 0xCF:
                    Visit("Restart at 8");
                    EndBranch();
                    break;


                case 0xD0:
                    Visit("Return if not carry");
                    break;
                case 0xD1:
                    Visit("Pop from stack into DE");
                    break;
                case 0xD2:
                    jumpAddress = GetDirectAddress();
                    Visit($"Jump to {GetFormattedDirectAddress()} if not carry", 2);
                    Branch(jumpAddress);
                    break;
                case 0xD3:
                    Visit("Illegal instruction");
                    break;
                case 0xD4:
                    jumpAddress = GetDirectAddress();
                    Visit($"Call {GetFormattedDirectAddress()} if not carry", 2);
                    Branch(jumpAddress);
                    break;
                case 0xD5:
                    Visit("Push to stack from DE");
                    break;
                case 0xD6:
                    Visit($"Subtract {GetFormattedDirectByte()} into A", 1);
                    break;
                case 0xD7:
                    Visit("Restart at 0x10");
                    EndBranch();
                    break;
                case 0xD8:
                    Visit("Return if carry");
                    break;
                case 0xD9:
                    Visit("Return and enable interrupt");
                    EndBranch();
                    break;
                case 0xDA:
                    jumpAddress = GetDirectAddress();
                    Visit($"Jump to {GetFormattedDirectAddress()} if carry", 2);
                    Branch(jumpAddress);
                    break;
                case 0xDB:
                    Visit("Illegal instruction");
                    break;
                case 0xDC:
                    jumpAddress = GetDirectAddress();
                    Visit($"Call {GetFormattedDirectAddress()} if carry", 2);
                    Branch(jumpAddress);
                    break;
                case 0xDD:
                    Visit("Illegal instruction");
                    break;
                case 0xDE:
                    Visit($"Subtract {GetFormattedDirectByte()} into A with carry", 1);
                    break;
                case 0xDF:
                    Visit("Restart at 0x18");
                    EndBranch();
                    break;


                case 0xE0:
                    Visit($"Load A into memory address 0x{0xFF00 + GetDirectByte():X4}", 1);
                    break;
                case 0xE1:
                    Visit("Pop from stack into HL");
                    break;
                case 0xE2:
                    Visit("Load A into memory address 0xFF00 + C");
                    break;
                case 0xE3:
                    Visit("Illegal instruction");
                    break;
                case 0xE4:
                    Visit("Illegal instruction");
                    break;
                case 0xE5:
                    Visit("Push HL to stack");
                    break;
                case 0xE6:
                    Visit($"AND {GetFormattedDirectByte()} into A", 1);
                    break;
                case 0xE7:
                    Visit("Restart at 0x20");
                    EndBranch();
                    break;
                case 0xE8:
                    Visit($"Add {(sbyte)GetDirectByte()} into SP", 1);
                    break;
                case 0xE9:
                    Visit("Jump to HL");
                    break;
                case 0xEA:
                    Visit($"Load A into memory address {GetFormattedDirectAddress()}", 2);
                    break;
                case 0xEB:
                    Visit("Illegal instruction");
                    break;
                case 0xEC:
                    Visit("Illegal instruction");
                    break;
                case 0xED:
                    Visit("Illegal instruction");
                    break;
                case 0xEE:
                    Visit($"XOR {GetFormattedDirectByte()} into A", 1);
                    break;
                case 0xEF:
                    Visit("Restart at 0x28");
                    EndBranch();
                    break;


                case 0xF0:
                    Visit($"Load from memory address 0x{0xFF00 + GetDirectByte():X4} into A", 1);
                    break;
                case 0xF1:
                    Visit("Pop from stack into AF");
                    break;
                case 0xF2:
                    Visit("Load from memory address 0xFF00 + C into A");
                    break;
                case 0xF3:
                    Visit("Disable interrupt");
                    break;
                case 0xF4:
                    Visit("Illegal instruction");
                    break;
                case 0xF5:
                    Visit("Push AF on to stack");
                    break;
                case 0xF6:
                    Visit($"OR {GetFormattedDirectByte()} into A", 1);
                    break;
                case 0xF7:
                    Visit("Restart at 0x30");
                    EndBranch();
                    break;
                case 0xF8:
                    Visit($"Load SP + {(sbyte)GetDirectByte()} into HL", 1);
                    break;
                case 0xF9:
                    Visit("Load HL into SP");
                    break;
                case 0xFA:
                    Visit($"Load from memory address {GetFormattedDirectAddress()} into A", 2);
                    break;
                case 0xFB:
                    Visit("Enable interrupt");
                    break;
                case 0xFC:
                    Visit("Illegal instruction");
                    break;
                case 0xFD:
                    Visit("Illegal instruction");
                    break;
                case 0xFE:
                    Visit($"Compare {GetFormattedDirectByte()} and A", 1);
                    break;
                case 0xFF:
                    Visit("Restart at 0x38");
                    EndBranch();
                    break;
            }
        }

        for (int i = 0; i < program.Length; i++)
        {
            if (isBranchStart[i])
            {
                outputFile.Write(
                    Encoding.ASCII.GetBytes(
                        $"------------------------ BEGIN ------------------------------{Environment.NewLine}"));
            }

            outputFile.Write(
                Encoding.ASCII.GetBytes(
                    $"0x{i:X4} | {allBytes[i]:X2}: {program[i]}{Environment.NewLine}"));

            if (alwaysJumps[i])
            {
                outputFile.Write(
                    Encoding.ASCII.GetBytes(
                        $"-------------------------------------------------------------{Environment.NewLine}"));
            }
            if (isBranchEnd[i])
            {
                outputFile.Write(
                    Encoding.ASCII.GetBytes(
                        $"------------------------- END -------------------------------{Environment.NewLine}"));
            }
        }
    }
}
