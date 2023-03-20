using System;
using Gameboi.Statics;
using static Gameboi.Statics.Frequencies;

namespace Gameboi.Memory.Specials;

public class OldDivWithNewState
{
    private Address counter;
    private readonly SystemState state;

    public Action OnWrite { get; private set; }

    public OldDivWithNewState(SystemState state, Action onWrite)
    {
        this.state = state;
        OnWrite = onWrite;
    }

    public Address Counter => counter;

    public Byte Read()
    {
        return ByteOperations.GetHighByte(counter);
    }

    public void Write(Byte _)
    {
        OnWrite?.Invoke();
        counter = 0;
    }

    public void Tick()
    {
        if (state.TicksElapsedThisFrame % ticksPerDivIncrement is 0)
        {
            if (state.Div++ is 0xff)
            {
                OnWrite?.Invoke();
            }
        }
    }
}

