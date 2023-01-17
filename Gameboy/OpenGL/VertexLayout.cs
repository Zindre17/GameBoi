using System.Collections.Generic;
using System.Linq;

namespace Gameboy.OpenGL;

public record VertexAttribute(
    int Count,
    bool Normalized
);

public class VertexLayout<T> where T : unmanaged
{
    private readonly List<VertexAttribute> attributes = new();

    public VertexLayout(params int[] counts)
    {
        foreach (var count in counts)
        {
            AddAttribute(count);
        }
    }

    public IReadOnlyList<VertexAttribute> Attributes => attributes.AsReadOnly();
    unsafe public uint Stride => (uint)(attributes.Sum(a => a.Count) * sizeof(T));

    public void AddAttribute(int count, bool Normalized = false)
    {
        attributes.Add(new(count, Normalized));
    }
}
