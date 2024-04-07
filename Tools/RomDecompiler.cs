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
    private RomLocation currentLocation;
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

            if (IsOutOfRomBankArea())
            {
                WriteComment(currentLocation, "Outside ROM bank area. Aborting branch.");
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
                    // TODO: this can be a bug if the jump is to a different bank
                    AddBranch(new(currentLocation.Bank, arg.Value), currentBranch.Label, $"Called from {instruction.Location}");
                }
            }

            if (instruction.IsRelativeJump())
            {
                if (instruction.Argument is Argument arg && autoBranching)
                {
                    AddBranch(currentLocation + arg.Value, currentBranch.Label, $"Jumped from {instruction.Location}");
                }
                state = State.Stopped;
            }

            if (instruction.IsAbsoluteJump())
            {
                if (instruction.Argument is Argument arg && autoBranching)
                {
                    // TODO: this can be a bug if the jump is to a different bank
                    AddBranch(new(currentLocation.Bank, arg.Value), currentBranch.Label, $"Jump from {instruction.Location}");
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
        AddVisitedAddress(currentLocation);
        var instruction = reader.ReadInstruction(currentLocation);
        currentLocation += instruction.Length;
        return instruction;
    }

    private void AddVisitedAddress(RomLocation location)
    {
        visitedAddresses.Add(location);
    }

    private void StartReadingNextBranch()
    {
        currentBranch = TakeOutNextBranch();
        currentLocation = currentBranch.Location;
        state = State.Reading;
        WriteLabel(currentLocation, currentBranch.ToString());
    }

    private enum State
    {
        Reading,
        Stopped
    }

    private bool IsOutOfRomBankArea() => currentLocation.Address is >= 0x4000;
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

    public static RomLocation operator +(RomLocation location, int offset) => new(location.Bank, location.Address + offset);
}

internal record Branch(RomLocation Location, string Label, string? Comment = null)
{
    public override string ToString() => Comment is null ? Label : $"{Label}: {Comment}";
};
