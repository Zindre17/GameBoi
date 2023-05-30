using Gameboi.Extensions;

namespace Gameboi.Sound;

public readonly struct SimpleNr52
{
    private readonly byte data;

    public SimpleNr52(byte data) => this.data = data;

    public bool IsSoundOn => data.IsBitSet(7);

    public bool IsChannelOn(int channel)
    {
        if (channel > 3)
        {
            throw new System.Exception();
        }
        return data.IsBitSet(channel);
    }
}
