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

internal class FileOutputDestination : IOutputDestination, IDisposable
{
    private readonly FileStream file;
    private readonly SortedList<FileOrderKey, FileLine> index = new();

    private readonly record struct FileOrderKey(RomLocation Location, bool IsComment) : IComparable<FileOrderKey>
    {
        public static bool operator <(FileOrderKey left, FileOrderKey right) => left.CompareTo(right) < 0;
        public static bool operator >(FileOrderKey left, FileOrderKey right) => left.CompareTo(right) > 0;
        public static bool operator <=(FileOrderKey left, FileOrderKey right) => left.CompareTo(right) <= 0;
        public static bool operator >=(FileOrderKey left, FileOrderKey right) => left.CompareTo(right) >= 0;

        public int CompareTo(FileOrderKey other)
        {
            var locationOrder = Location.CompareTo(other.Location);
            if (locationOrder is not 0)
            {
                return locationOrder;
            }
            return other.IsComment.CompareTo(IsComment);
        }
    }

    private record FileLine(int Position, int Length)
    {
        public int LineEnd => Position + Length;

        public FileLine Move(int offset) => new(Position + offset, Length);

        internal FileLine Extend(int length) => new(Position, Length + length);
    }
    public FileOutputDestination(string filePath)
    {
        file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        file.Seek(0, SeekOrigin.End);
    }

    public void Dispose()
    {
        file.Flush();
        file.Dispose();
    }

    public void WriteInstruction(Instruction instruction)
    {
        var newKey = new FileOrderKey(instruction.Location, false);
        if (index.ContainsKey(newKey))
        {
            return;
        }

        var text = instruction.ToString() + "\n";

        if (!index.Any() || index.Last().Key <= newKey)
        {
            file.Seek(0, SeekOrigin.End);
        }
        else
        {
            var insertPosition = ShiftContentBelowBy(newKey, text.Length);

            file.Seek(insertPosition, SeekOrigin.Begin);
        }

        index.TryAdd(newKey, new((int)file.Position, text.Length));
        file.Write(Encoding.UTF8.GetBytes(text));
    }

    public void WriteComment(RomLocation location, string text)
    {
        var newKey = new FileOrderKey(location, true);
        text += "\n";
        if (index.ContainsKey(newKey))
        {
            // Append to existing comment
            var fileLine = index[newKey];
            ShiftContentBelowBy(newKey, text.Length);
            file.Seek(fileLine.LineEnd, SeekOrigin.Begin);
            file.Write(Encoding.UTF8.GetBytes(text));
            index[newKey] = fileLine.Extend(text.Length);
        }
        else
        {
            // Insert new comment
            if (!index.Any() || index.Last().Key <= newKey)
            {
                file.Seek(0, SeekOrigin.End);
            }
            else
            {
                var insertPosition = ShiftContentBelowBy(newKey, text.Length);
                file.Seek(insertPosition, SeekOrigin.Begin);
            }

            index.TryAdd(newKey, new((int)file.Position, text.Length));
            file.Write(Encoding.UTF8.GetBytes(text));
        }
    }

    private int ShiftContentBelowBy(FileOrderKey target, int length)
    {
        var insertPosition = (int)file.Length;
        file.SetLength(file.Length + length);
        foreach (var i in Enumerable.Range(0, index.Count).Reverse())
        {
            var (key, fileLine) = index.ElementAt(i);
            if (key > target)
            {
                insertPosition = fileLine.Position;
                index[key] = MoveLine(fileLine, length);
            }
            else
            {
                break;
            }
        }
        return insertPosition;
    }

    private FileLine MoveLine(FileLine fileLine, int offset)
    {
        var lineBytes = ReadLine(fileLine);
        var newPosition = fileLine.Position + offset;
        file.Seek(newPosition, SeekOrigin.Begin);
        file.Write(lineBytes, 0, fileLine.Length);

        return fileLine.Move(offset);
    }

    private byte[] ReadLine(FileLine fileLine)
    {
        var lineBytes = new byte[fileLine.Length];
        file.Seek(fileLine.Position, SeekOrigin.Begin);
        file.Read(lineBytes, 0, fileLine.Length);
        return lineBytes;
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
