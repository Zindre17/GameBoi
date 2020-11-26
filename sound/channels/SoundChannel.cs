
public abstract class SoundChannel : Hardware
{
    protected NR52 nr52;
    public SoundChannel(NR52 nr52) => this.nr52 = nr52;

    public abstract override void Connect(Bus bus);

}