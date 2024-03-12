using System.Text;

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
    public static DecompilerWriter CreateDuoWriter(string filePath) => new(new DuoOutputDestination(new FileOutputDestination(filePath), new ConsoleOutputDestination()));

    public void WriteComment(string comment) => destination.WriteLine(comment);

    public void WriteLabel(string label)
    {
        var fillLength = (MaxLabelWidth - label.Length) / 2;
        var odd = (MaxLabelWidth - label.Length) % 2 == 1;
        var fill = new string('-', fillLength - 1);
        destination.WriteLine($"\n{fill} {label} {fill}{(odd ? "-" : "")}");
    }

    public void WriteInstruction(Instruction instruction) => destination.WriteLine(instruction.ToString());

    public void Dispose()
    {
        destination.Dispose();
    }
}

internal interface IOutputDestination : IDisposable
{
    void WriteLine(string text);
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

    public void WriteLine(string text)
    {
        first.WriteLine(text);
        second.WriteLine(text);
    }
}

internal class FileOutputDestination : IOutputDestination, IDisposable
{
    private readonly FileStream file;
    public FileOutputDestination(string filePath)
    {
        file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        file.Seek(0, SeekOrigin.End);
    }

    public void Dispose()
    {
        file.Flush();
        file.Dispose();
    }

    public void WriteLine(string text)
    {
        file.Write(Encoding.UTF8.GetBytes(text + "\n"));
    }
}

internal class ConsoleOutputDestination : IOutputDestination
{
    public void Dispose()
    { }

    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }
}
