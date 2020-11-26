public class FrequencyLow : Register
{
    public override Byte Read() => 0xFF;

    public Byte LowBits => data;

}