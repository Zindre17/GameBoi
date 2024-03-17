using System.Text;

namespace Gameboi.Tools;

internal class FileOutputDestination : IOutputDestination, IDisposable
{
    private readonly FileStream file;
    private readonly SortedList<FileOrderKey, FileLine> index = new();

    public FileOutputDestination(string filePath)
    {
        file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        IndexExistingContent();
        file.Seek(0, SeekOrigin.End);
    }

    private void IndexExistingContent()
    {
        file.Seek(0, SeekOrigin.Begin);

        var line = ReadLine();

        string commentForNextInstruction = string.Empty;
        int startPosition = 0;

        while (line.Length > 0)
        {
            if (line.StartsWith('-') || line.StartsWith('\n'))
            {
                commentForNextInstruction += line;
            }
            else
            {
                var instruction = RomLocation.Parse(line);
                if (commentForNextInstruction.Length > 0)
                {
                    var commentLine = new FileLine(startPosition, commentForNextInstruction.Length);
                    index.TryAdd(new(instruction, true), commentLine);
                    index.TryAdd(new(instruction, false), new(commentLine.LineEnd, line.Length));
                }
                else
                {
                    index.TryAdd(new(instruction, false), new(startPosition, line.Length));
                }
                commentForNextInstruction = string.Empty;
                startPosition = (int)file.Position;
            }
            line = ReadLine();
        }
    }

    private string ReadLine()
    {
        var currentLine = new List<byte>();
        while (file.Position < file.Length)
        {
            var symbol = file.ReadByte();
            if (symbol is -1)
            {
                break;
            }
            currentLine.Add((byte)symbol);
            if ((char)symbol is '\n')
            {
                break;
            }
        }
        return Encoding.UTF8.GetString(currentLine.ToArray());
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
}
