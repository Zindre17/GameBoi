public class LFSR
{
    private int size;
    private int state;

    public LFSR(int size)
    {
        this.size = size;
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < size; i++)
        {
            int bit = (1 << i);
            state |= bit;
        }
    }

    public bool Tick()
    {
        int bit0 = state & 1;
        int next = bit0 ^ ((state & 2) >> 1);

        state = state >> 1;

        if (next != 0)
            state = state | (1 << (size - 1));

        if (state == 0) throw new System.Exception();
        return bit0 != 0;
    }

}