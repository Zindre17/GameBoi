namespace Gameboi.Tools;

internal class RomDecompiler
{

    public RomDecompiler(string romPath)
    {
        if (!File.Exists(romPath))
        {
            throw new FileNotFoundException("The file does not exist", romPath);
        }
        file = File.OpenRead(romPath);
    }

    private readonly Stack<Branch> branches = new();
    private State state = State.Stopped;
    private readonly FileStream file;

    private bool InProgress => state is State.Reading || branches.Any();

    private void AddBranch(int address, string label) => branches.Push(new(address, label));
    private Branch TakeOutNextBranch() => branches.Pop();

    private void AddEntryPoint() => AddBranch(0x100, "EntryPoint");

    private void AddRestartPoints()
    {
        AddBranch(0x0, "Restart 0x00");
        AddBranch(0x8, "Restart 0x08");
        AddBranch(0x10, "Restart 0x10");
        AddBranch(0x18, "Restart 0x18");
        AddBranch(0x20, "Restart 0x20");
        AddBranch(0x28, "Restart 0x28");
        AddBranch(0x30, "Restart 0x30");
        AddBranch(0x38, "Restart 0x38");
    }

    private void AddInterruptPoints()
    {
        AddBranch(0x40, "Vblank");
        AddBranch(0x48, "LcdStat");
        AddBranch(0x50, "Timer");
        AddBranch(0x58, "Serial");
        AddBranch(0x60, "Joypad");
    }

    public void InterpretRom()
    {
        AddEntryPoint();
        AddRestartPoints();
        AddInterruptPoints();

        while (InProgress)
        {
            if (state is State.Stopped)
            {
                StartReadingNextBranch();
            }

            var position = file.Position;
            var opCode = ReadOpCode();
            var argument = ReadArgument(opCode);

            OutputDecompiledOperation(position, opCode, argument);

            if (IsJump(opCode))
            {
                if (argument is Argument arg)
                {
                    AddBranch(arg.Value, "Jump from 0x" + position.ToString("X4"));
                }
                state = State.Stopped;
            }
            if (IsReturn(opCode))
            {
                state = State.Stopped;
            }
        }
    }

    private static void OutputDecompiledOperation(long position, int opCode, IArgument argument)
    {
        var assemblyString = AssemblyConverter.ToString(opCode, argument);
        Console.WriteLine($"0x{position:X4}: 0x{opCode:X2} - {assemblyString}");
    }

    private int ReadOpCode() => ReadByte();

    private IArgument ReadArgument(int opCode)
    {
        var argumentType = AssemblyConverter.GetInstructionArgumentType(opCode);
        return argumentType switch
        {
            ArgumentType.None => NoneArgument.Instance,
            ArgumentType.Byte => new Argument(ReadByte()),
            ArgumentType.SignedByte => new Argument(ReadSignedByte()),
            ArgumentType.Address => new Argument(ReadAddress()),
            _ => throw new NotImplementedException()
        };
    }

    private int ReadByte() => file.ReadByte();
    private int ReadSignedByte() => (sbyte)file.ReadByte();
    private int ReadAddress() => file.ReadByte() | file.ReadByte() << 8;

    private void StartReadingNextBranch()
    {
        var branch = TakeOutNextBranch();
        Console.WriteLine($"--------- {branch.Label} ---------");
        file.Position = branch.Address;
        state = State.Reading;
    }

    private enum State
    {
        Reading,
        Stopped
    }

    private static bool IsJump(int opCode) => opCode is 0x18 or 0xc3 or 0xc7 or 0xcf or 0xd7 or 0xdf or 0xe7 or 0xe9 or 0xef or 0xf7 or 0xff;
    private static bool IsReturn(int opCode) => opCode is 0xc9 or 0xd9;
}

internal record Branch(int Address, string Label);
