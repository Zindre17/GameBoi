namespace Gameboi.Extensions;

public static class ByteArrayExtensions
{
    public static void Clear(this byte[] memory)
    {
        for (int i = 0; i < memory.Length; i++)
        {
            memory[i] = 0;
        }
    }
}
