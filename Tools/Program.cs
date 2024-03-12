// See https://aka.ms/new-console-template for more information
using Gameboi.Tools;
using Giinn;

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


var interactive = args.Any(a => a is "--interactive" or "-i");
if (interactive)
{
    Console.WriteLine("Interactive mode started.");

    int address = 0;

    Input.Enter("Where would you like to start?",
         res => int.TryParse(res, System.Globalization.NumberStyles.AllowHexSpecifier, null, out address),
         "Invalid address - Must be a valid hex number [0-9A-Fa-f]{1,4}");

    using var decompiler = new RomDecompiler(new(file), DecompilerWriter.CreateConsoleWriter());
    Console.CancelKeyPress += (_, _) => decompiler.Dispose();
    decompiler.DisableAutoBranching();
    decompiler.AddBranch(address, "Manual Start");
    decompiler.InterpretRom();

    while (true)
    {
        var quit = Input.Enter("\nWhere would you like to go next?",
            res => res is "q" || int.TryParse(res, System.Globalization.NumberStyles.AllowHexSpecifier, null, out address),
            "Invalid address - Must be a valid hex number [0-9A-Fa-f]{1,4}\nType 'q' to quit.") is "q";
        if (quit) break;
        decompiler.AddBranch(address, "Manual Jump");
        decompiler.InterpretRom();
    }

    return;
}
else
{
    var singleBlock = args.Any(a => a is "--single");
    var singleStartingAddress = args.FirstOrDefault(a => a.StartsWith("--start="))?.Split("=")[1];

    // var codeGen = new HumanReadableCodeGenerator();
    // codeGen.InterpretRom(file);

    using var decompiler = new RomDecompiler(new(file), DecompilerWriter.CreateConsoleWriter());
    Console.CancelKeyPress += (_, _) => decompiler.Dispose();
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
}
