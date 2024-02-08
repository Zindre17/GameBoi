// See https://aka.ms/new-console-template for more information
using Gameboi.Tools;

var file = args[0];

if (!File.Exists(file))
{
    Console.WriteLine("File does not exist");
    return;
}

// var codeGen = new HumanReadableCodeGenerator();
// codeGen.InterpretRom(file);

using var decompiler = new RomDecompiler(new(file));
decompiler.InterpretRom();
