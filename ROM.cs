class ROM : Memory
{
    public ROM(ushort size) : base(size, true) { }
    public ROM(byte[] rom) : base((ushort)rom.Length, true)
    {
        memory = new byte[rom.Length];
        rom.CopyTo(memory, 0);
    }
}