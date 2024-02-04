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

    private readonly Queue<Branch> branches = new();
    private State state = State.Stopped;
    private readonly FileStream file;

    private bool InProgress => state is State.Reading || branches.Any();

    private void EnqueueBranch(int address, string label) => branches.Enqueue(new(address, label));
    private void EnqueueEntryPoint() => EnqueueBranch(0x100, "EntryPoint");

    private void EnqueueRestartPoints()
    {
        EnqueueBranch(0x0, "Restart 0x00");
        EnqueueBranch(0x8, "Restart 0x08");
        EnqueueBranch(0x10, "Restart 0x10");
        EnqueueBranch(0x18, "Restart 0x18");
        EnqueueBranch(0x20, "Restart 0x20");
        EnqueueBranch(0x28, "Restart 0x28");
        EnqueueBranch(0x30, "Restart 0x30");
        EnqueueBranch(0x38, "Restart 0x38");
    }

    private void EnqueueInterruptPoints()
    {
        EnqueueBranch(0x40, "Vblank");
        EnqueueBranch(0x48, "LcdStat");
        EnqueueBranch(0x50, "Timer");
        EnqueueBranch(0x58, "Serial");
        EnqueueBranch(0x60, "Joypad");
    }

    public void InterpretRom()
    {
        EnqueueEntryPoint();
        EnqueueRestartPoints();
        EnqueueInterruptPoints();

        while (InProgress)
        {
            if (state is State.Stopped)
            {
                StartReadingNextBranch();
            }

            var opCode = ReadByte();
            var argument = GetInstructionArgument(opCode);
            var assemblyString = AssemblyConverter.ToString(opCode, argument);
            Console.WriteLine($"0x{file.Position - 1:X4}: 0x{opCode:X2} - {assemblyString}");

            if (IsJump(opCode))
            {
                state = State.Stopped;
            }
            if (IsReturn(opCode))
            {
                state = State.Stopped;
            }
        }
    }

    private IArgument GetInstructionArgument(int opCode)
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
        var branch = branches.Dequeue();
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
