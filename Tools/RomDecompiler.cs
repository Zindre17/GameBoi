namespace Gameboi.Tools;

public class RomDecompiler
{

    public RomDecompiler(string romPath)
    {
        if (!File.Exists(romPath))
        {
            throw new FileNotFoundException("The file does not exist", romPath);
        }
        file = File.OpenRead(romPath);
    }

    private Queue<int> branches = new();
    private State state = State.Stopped;
    private FileStream file;

    private bool InProgress => state is State.Reading || branches.Any();

    private void EnqueueEntryPoint() => branches.Enqueue(0x100);

    private void EnqueueRestartPoints()
    {
        branches.Enqueue(0x0);
        branches.Enqueue(0x8);
        branches.Enqueue(0x10);
        branches.Enqueue(0x18);
        branches.Enqueue(0x20);
        branches.Enqueue(0x28);
        branches.Enqueue(0x30);
        branches.Enqueue(0x38);
    }

    private void EnqueueInterruptPoints()
    {
        branches.Enqueue(0x40);
        branches.Enqueue(0x48);
        branches.Enqueue(0x50);
        branches.Enqueue(0x58);
        branches.Enqueue(0x60);
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

            var opCode = file.ReadByte();
            var assemblyString = AssemblyConverter.ToString((byte)opCode);
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

    private void StartReadingNextBranch()
    {
        Console.WriteLine("--------- Starting new branch ---------");
        file.Position = branches.Dequeue();
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
