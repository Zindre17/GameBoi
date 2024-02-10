using System.Text;

namespace Gameboi.Tools;

internal class DecompilerWriter
{
    private readonly IOutputDestination destination;

    private const int MaxLabelWidth = 50;
    private DecompilerWriter(IOutputDestination destination)
    {
        this.destination = destination;
    }

    public static DecompilerWriter CreateFileWriter(string filePath) => new(new FileOutputDestination(filePath));
    public static DecompilerWriter CreateConsoleWriter() => new(new ConsoleOutputDestination());

    public void WriteComment(string comment)
    {
        destination.WriteLine(comment);
    }

    public void WriteLabel(string label)
    {
        var fillLength = (MaxLabelWidth - label.Length) / 2;
        var odd = (MaxLabelWidth - label.Length) % 2 == 1;
        var fill = new string('-', fillLength - 1);
        destination.WriteLine($"\n{fill} {label} {fill}{(odd ? "-" : "")}");
    }

    public void WriteInstruction(Instruction instruction)
    {
        var code = AssemblyConverter.Decompile(instruction.OpCode, instruction.Argument);
        destination.WriteLine($"0x{instruction.Address:X4}: 0x{instruction.OpCode:X2} - {code}");
    }
}

internal interface IOutputDestination : IDisposable
{
    void WriteLine(string text);
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
