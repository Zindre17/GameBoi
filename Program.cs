using System;

namespace GB_Emulator
{
    class Program
    {
        static void Add(ref byte target)
        {
            target++;
        }
        static void Main(string[] args)
        {
            byte a = 0x01;
            Console.WriteLine(a);
            Add(ref a);
            Add(ref a);
            Console.WriteLine(a);
            Add(ref a);
            Console.WriteLine(a);


        }
    }
}
