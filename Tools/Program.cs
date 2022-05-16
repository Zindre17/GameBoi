// See https://aka.ms/new-console-template for more information
using GB_Emulator.Tools;

var file = args[0];

var codeGen = new HumanReadableCodeGenerator();
codeGen.InterpretRom(file);
