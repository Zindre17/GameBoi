// See https://aka.ms/new-console-template for more information
using Gameboi.Tools;

var file = args.LastOrDefault();

if (string.IsNullOrWhiteSpace(file))
{
    Console.WriteLine("No file specified");
    return;
}

if (!File.Exists(file))
{
    Console.WriteLine("File does not exist");
    return;
}

var singleBlock = args.Any(a => a is "--single");
var singleStartingAddress = args.FirstOrDefault(a => a.StartsWith("--start="))?.Split("=")[1];

// var codeGen = new HumanReadableCodeGenerator();
// codeGen.InterpretRom(file);

using var decompiler = new RomDecompiler(new(file), DecompilerWriter.CreateConsoleWriter());
if (singleBlock)
{
    decompiler.DisableAutoBranching();
    decompiler.AddBranch(int.Parse(singleStartingAddress ?? throw new ArgumentNullException(singleStartingAddress, "Must specify a --start= parameter when --single is set."), System.Globalization.NumberStyles.AllowHexSpecifier), "EntryPoint");
}
else
{
    decompiler.AddAllKnownEntryPoints();
}
// using var decompiler = new RomDecompiler(new(file), DecompilerWriter.CreateFileWriter("red.txt"));
decompiler.InterpretRom();
