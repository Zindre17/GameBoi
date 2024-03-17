namespace Gameboi.Tools;

internal class RomDecompiler : IDisposable
{
    public RomDecompiler(RomReader reader, DecompilerWriter writer)
    {
        this.reader = reader;
        this.writer = writer;
    }

    private readonly Stack<Branch> branches = new();
    private readonly HashSet<RomLocation> visitedAddresses = new();
    private State state = State.Stopped;
    private readonly RomReader reader;
    private readonly DecompilerWriter writer;
    private Branch currentBranch = null!;
    private int programCounter;
    private bool autoBranching = true;

    private bool InProgress => state is State.Reading || branches.Any();

    public void AddBranch(RomLocation location, string label, string? comment = null)
    {
        if (visitedAddresses.Contains(location))
        {
            return;
        }
        branches.Push(new(location, label, comment));
    }
    private Branch TakeOutNextBranch() => branches.Pop();

    private void AddEntryPoint() => AddBranch(new(0, 0x100), "EntryPoint");

    private void AddRestartPoints(int bank)
    {
        AddBranch(new(bank, 0x0), "Restart 0x00");
        AddBranch(new(bank, 0x8), "Restart 0x08");
        AddBranch(new(bank, 0x10), "Restart 0x10");
        AddBranch(new(bank, 0x18), "Restart 0x18");
        AddBranch(new(bank, 0x20), "Restart 0x20");
        AddBranch(new(bank, 0x28), "Restart 0x28");
        AddBranch(new(bank, 0x30), "Restart 0x30");
        AddBranch(new(bank, 0x38), "Restart 0x38");
    }

    private void AddInterruptPoints(int bank)
    {
        AddBranch(new(bank, 0x40), "Vblank");
        AddBranch(new(bank, 0x48), "LcdStat");
        AddBranch(new(bank, 0x50), "Timer");
        AddBranch(new(bank, 0x58), "Serial");
        AddBranch(new(bank, 0x60), "Joypad");
    }

    public void AddAllKnownEntryPoints()
    {
        AddEntryPoint();
        AddRestartPoints(0);
        AddInterruptPoints(0);
    }

    public void InterpretRom()
    {
        while (InProgress)
        {
            if (state is State.Stopped)
            {
                StartReadingNextBranch();
            }

            if (IsRamAddressArea())
            {
                WriteComment(new(0, programCounter), "RAM area. Aborting branch.");
                state = State.Stopped;
                continue;
            }

            if (IsOutOfBusRange())
            {
                WriteComment(new(0, programCounter), "Out of bus range");
                state = State.Stopped;
                continue;
            }

            var instruction = ReadNextInstruction();

            if (IsProbablyUnusedMemory(instruction))
            {
                WriteComment(instruction.Location, "Likeley bug in decompiler (if not an interrupt vector). 0xFF (restart 0x38) is default rom value for unused memory. Stopping.");
                state = State.Stopped;
                continue;
            }

            WriteInstruction(instruction);

            if (instruction.IsCall())
            {
                if (instruction.Argument is Argument arg && autoBranching)
                {
                    AddBranch(GetRomLocation(arg.Value), currentBranch.Label, $"Called from {instruction.Location}");
                }
            }

            if (instruction.IsRelativeJump())
            {
                if (instruction.Argument is Argument arg && autoBranching)
                {
                    AddBranch(GetRomLocation(arg.Value + programCounter), currentBranch.Label, $"Jumped from {instruction.Location}");
                }
                state = State.Stopped;
            }

            if (instruction.IsAbsoluteJump())
            {
                if (instruction.Argument is Argument arg && autoBranching)
                {
                    AddBranch(GetRomLocation(arg.Value), currentBranch.Label, $"Jump from {instruction.Location}");
                }
                state = State.Stopped;
            }

            if (instruction.IsReturn())
            {
                state = State.Stopped;
            }
        }
    }

    private void WriteInstruction(Instruction instruction) => writer.WriteInstruction(instruction);
    private void WriteLabel(RomLocation location, string label) => writer.WriteLabel(location, label);
    private void WriteComment(RomLocation location, string message) => writer.WriteComment(location, message);

    private Instruction ReadNextInstruction()
    {
        AddVisitedAddress(GetRomLocation());
        var instruction = reader.ReadInstruction(GetRomLocation());
        programCounter += instruction.Length;
        return instruction;
    }

    private RomLocation GetRomLocation() => GetRomLocation(programCounter);

    private static RomLocation GetRomLocation(int address)
    {
        if (address > 0x8000) throw new NotImplementedException();

        if (address > 0x4000) return new RomLocation(1, address - 0x4000);

        return new RomLocation(0, address);
    }

    private int GetAddress(RomLocation location)
    {
        if (location.Bank > 1) throw new NotImplementedException();
        return location.Bank * 0x4000 + location.Address;
    }

    private void AddVisitedAddress(RomLocation location)
    {
        visitedAddresses.Add(location);
    }

    private void StartReadingNextBranch()
    {
        currentBranch = TakeOutNextBranch();
        programCounter = GetAddress(currentBranch.Location);
        state = State.Reading;
        WriteLabel(GetRomLocation(), currentBranch.ToString());
    }

    private enum State
    {
        Reading,
        Stopped
    }

    private bool IsRamAddressArea() => programCounter is >= 0x8000;
    private bool IsOutOfBusRange() => programCounter is < 0 or > 0xffff;
    private bool IsProbablyUnusedMemory(Instruction instruction) => IsStartOfBranch(instruction.Location) && instruction.OpCode is 0xff;
    private bool IsStartOfBranch(RomLocation location) => location == currentBranch.Location;

    public void Dispose()
    {
        reader.Dispose();
        writer.Dispose();
    }

    internal void DisableAutoBranching()
    {
        autoBranching = false;
    }
}

internal readonly record struct RomLocation(int Bank, int Address) : IComparable<RomLocation>
{
    public static RomLocation FromBusAddress(int address) => new(address / 0x4000, address % 0x4000);

    public static bool operator >(RomLocation x, RomLocation y) => x.CompareTo(y) > 0;
    public static bool operator <(RomLocation x, RomLocation y) => x.CompareTo(y) < 0;
    public static bool operator >=(RomLocation x, RomLocation y) => x.CompareTo(y) >= 0;
    public static bool operator <=(RomLocation x, RomLocation y) => x.CompareTo(y) <= 0;

    public override string ToString() => $"0x{Bank:X2}-{Address:X4}";

    public int CompareTo(RomLocation other)
    {
        if (Bank == other.Bank)
        {
            return Address.CompareTo(other.Address);
        }
        return Bank.CompareTo(other.Bank);
    }

    internal static RomLocation Parse(string line)
    {
        var parts = line.Split('-');
        return new(int.Parse(parts[0][2..4], System.Globalization.NumberStyles.HexNumber), int.Parse(parts[1][..4], System.Globalization.NumberStyles.HexNumber));
    }
}

internal record Branch(RomLocation Location, string Label, string? Comment = null)
{
    public override string ToString() => Comment is null ? Label : $"{Label}: {Comment}";
};
