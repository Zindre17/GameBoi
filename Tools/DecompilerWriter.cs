namespace Gameboi.Tools;

internal class DecompilerWriter : IDisposable
{
    private readonly IOutputDestination destination;

    private const int MaxLabelWidth = 50;
    private DecompilerWriter(IOutputDestination destination)
    {
        this.destination = destination;
    }

    public static DecompilerWriter CreateFileWriter(string filePath) => new(new FileOutputDestination(filePath));
    public static DecompilerWriter CreateConsoleWriter() => new(new ConsoleOutputDestination());
    public static DecompilerWriter CreateDuoWriter(string filePath) => new(new DuoOutputDestination(new SplitFileOutputDestination(filePath), new ConsoleOutputDestination()));

    public void WriteComment(RomLocation location, string comment) => destination.WriteComment(location, comment);

    public void WriteLabel(RomLocation location, string label)
    {
        var fillLength = (MaxLabelWidth - label.Length) / 2;
        var odd = (MaxLabelWidth - label.Length) % 2 == 1;
        var fill = new string('-', fillLength - 1);
        destination.WriteComment(location, $"\n{fill} {label} {fill}{(odd ? "-" : "")}");
    }

    public void WriteInstruction(Instruction instruction) => destination.WriteInstruction(instruction);

    public void Dispose()
    {
        destination.Dispose();
    }
}

internal interface IOutputDestination : IDisposable
{
    void WriteInstruction(Instruction instruction);
    void WriteComment(RomLocation location, string text);
}

internal class DuoOutputDestination : IOutputDestination
{
    private readonly IOutputDestination first;
    private readonly IOutputDestination second;

    public DuoOutputDestination(IOutputDestination first, IOutputDestination second)
    {
        this.first = first;
        this.second = second;
    }

    public void Dispose()
    {
        first.Dispose();
        second.Dispose();
    }

    public void WriteComment(RomLocation location, string text)
    {
        first.WriteComment(location, text);
        second.WriteComment(location, text);
    }

    public void WriteInstruction(Instruction instruction)
    {
        first.WriteInstruction(instruction);
        second.WriteInstruction(instruction);
    }
}

internal class ConsoleOutputDestination : IOutputDestination
{
    public void Dispose()
    { }

    public void WriteInstruction(Instruction instruction)
    {
        Console.WriteLine(instruction.ToString());
    }

    public void WriteComment(RomLocation _, string text)
    {
        Console.WriteLine(text);
    }
}
