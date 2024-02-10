namespace Gameboi.Tools;

internal class RomDecompiler : IDisposable
{
    public RomDecompiler(RomReader reader, DecompilerWriter writer)
    {
        this.reader = reader;
        this.writer = writer;
    }

    private readonly Stack<Branch> branches = new();
    private readonly HashSet<int> visitedAddresses = new();
    private State state = State.Stopped;
    private readonly RomReader reader;
    private readonly DecompilerWriter writer;
    private Branch currentBranch = null!;
    private int programCounter;

    private bool InProgress => state is State.Reading || branches.Any();

    private void AddBranch(int address, string label, string? comment = null)
    {
        if (visitedAddresses.Contains(address))
        {
            return;
        }
        branches.Push(new(address, label, comment));
    }
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

            var instructionStartPosition = programCounter;

            if (IsRamAddressArea(instructionStartPosition))
            {
                WriteComment(instructionStartPosition, "RAM area. Aborting branch.");
                state = State.Stopped;
                continue;
            }

            if (IsOutOfBusRange(instructionStartPosition))
            {
                WriteComment(instructionStartPosition, "Out of bus range");
                state = State.Stopped;
                continue;
            }

            var opCode = ReadOpCode();

            if (IsProbablyUnusedMemory(instructionStartPosition, opCode))
            {
                WriteComment(instructionStartPosition, "Likeley bug in decompiler (if not an interrupt vector). 0xFF (restart 0x38) is default rom value for unused memory. Stopping.");
                state = State.Stopped;
                continue;
            }

            var argument = ReadArgument(opCode);

            WriteInstruction(new(instructionStartPosition, opCode, argument));

            if (IsCall(opCode))
            {
                if (argument is Argument arg)
                {
                    AddBranch(arg.Value, currentBranch.Label, "Called from 0x" + instructionStartPosition.ToString("X4"));
                }
            }

            if (IsRelativeJump(opCode))
            {
                if (argument is Argument arg)
                {
                    AddBranch(arg.Value + programCounter, currentBranch.Label, "Jumped from 0x" + instructionStartPosition.ToString("X4"));
                }
                state = State.Stopped;
            }

            if (IsAbsoluteJump(opCode))
            {
                if (argument is Argument arg)
                {
                    AddBranch(arg.Value, currentBranch.Label, "Jump from 0x" + instructionStartPosition.ToString("X4"));
                }
                state = State.Stopped;
            }

            if (IsReturn(opCode))
            {
                state = State.Stopped;
            }
        }
    }

    private void WriteInstruction(Instruction instruction) => writer.WriteInstruction(instruction);
    private void WriteLabel(string label) => writer.WriteLabel(label);
    private void WriteComment(int position, string message) => WriteComment($"0x{position:X4}: {message}");
    private void WriteComment(string message) => writer.WriteComment(message);

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

    private int ReadByte()
    {
        AddVisitedAddress(programCounter);
        var location = GetRomLocation(programCounter++);
        return reader.ReadByte(location);
    }

    private static RomLocation GetRomLocation(int address)
    {
        if (address > 0x8000) throw new NotImplementedException();

        if (address > 0x4000) return new RomLocation(1, address - 0x4000);

        return new RomLocation(0, address);
    }

    private int ReadSignedByte() => (sbyte)ReadByte();
    private int ReadAddress()
    {
        var location = GetRomLocation(programCounter);
        visitedAddresses.Add(programCounter++);
        visitedAddresses.Add(programCounter++);
        return reader.ReadAddress(location);
    }

    private void AddVisitedAddress(int address)
    {
        visitedAddresses.Add(address);
    }

    private void StartReadingNextBranch()
    {
        currentBranch = TakeOutNextBranch();
        WriteLabel(currentBranch.ToString());
        programCounter = currentBranch.Address;
        state = State.Reading;
    }

    private enum State
    {
        Reading,
        Stopped
    }

    private static bool IsRelativeJump(int opCode) => opCode is 0x18;
    private static bool IsAbsoluteJump(int opCode) => opCode is 0xc3 or 0xcf or 0xe9;
    private static bool IsReturn(int opCode) => opCode is 0xc9 or 0xd9;
    private static bool IsCall(int opCode) => opCode is 0xc4 or 0xc7 or 0xcc or 0xcd or 0xcf or 0xd4 or 0xd7 or 0xdc or 0xdf or 0xe7 or 0xef or 0xf7 or 0xff;
    private static bool IsRamAddressArea(int address) => address is >= 0x8000;
    private static bool IsOutOfBusRange(int address) => address is < 0 or > 0xffff;
    private bool IsProbablyUnusedMemory(int address, int opCode) => IsStartOfBranch(address) && opCode is 0xff;
    private bool IsStartOfBranch(int address) => address == currentBranch.Address;

    public void Dispose()
    {
        reader.Dispose();
    }
}

internal readonly record struct RomLocation(int Bank, int Address);

internal record Branch(int Address, string Label, string? Comment = null)
{
    public override string ToString() => Comment is null ? Label : $"{Label}: {Comment}";
};
