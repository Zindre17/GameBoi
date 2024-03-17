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

var output = args.FirstOrDefault(a => a.StartsWith("--output=") || a.StartsWith("-o="))?.Split("=")[1] ?? "output";
var interactive = args.Any(a => a is "--interactive" or "-i");
if (interactive)
{
    Console.WriteLine("Interactive mode started.");

    int address = 0;
    int bank = 0;
    Input.Enter("Where would you like to start?",
         res => ParseRomLocation(res, out bank, out address),
         "Invalid address - Must be a valid hex number [0-9A-Fa-f]{1,4}");

    using var decompiler = new RomDecompiler(new(file), DecompilerWriter.CreateDuoWriter(output));
    Console.CancelKeyPress += (_, _) => decompiler.Dispose();
    decompiler.DisableAutoBranching();
    decompiler.AddBranch(new(bank, address), "Manual Start");
    decompiler.InterpretRom();

    while (true)
    {
        var quit = Input.Enter("\nWhere would you like to go next?",
            res => res is "q" || ParseRomLocation(res, out bank, out address),
            "Invalid address - Must be a valid hex number [0-9A-Fa-f]{1,4}\nType 'q' to quit.") is "q";
        if (quit) break;
        decompiler.AddBranch(new(bank, address), "Manual Jump");
        decompiler.InterpretRom();
    }

    return;
}
else
{
    var singleBlock = args.Any(a => a is "--single");
    var singleStartingAddress = args.FirstOrDefault(a => a.StartsWith("--start="))?.Split("=")[1];

    using var decompiler = new RomDecompiler(new(file), DecompilerWriter.CreateConsoleWriter());
    Console.CancelKeyPress += (_, _) => decompiler.Dispose();
    if (singleBlock)
    {
        decompiler.DisableAutoBranching();
        if (!ParseRomLocation(singleStartingAddress, out var bank, out var address))
        {
            throw new ArgumentNullException(singleStartingAddress, "Must specify a --start= parameter when --single is set.");
        }
        decompiler.AddBranch(new(bank, address), "EntryPoint");
    }
    else
    {
        decompiler.AddAllKnownEntryPoints();
    }
    decompiler.InterpretRom();
}

static bool ParseRomLocation(string? location, out int bank, out int address)
{
    bank = 0;
    address = 0;
    if (string.IsNullOrWhiteSpace(location)) return false;
    if (location.Contains('-'))
    {
        var parts = location.Split('-');
        if (parts.Length is not 2) return false;
        if (!int.TryParse(parts[0], System.Globalization.NumberStyles.AllowHexSpecifier, null, out bank)) return false;
        if (!int.TryParse(parts[1], System.Globalization.NumberStyles.AllowHexSpecifier, null, out address)) return false;
        return true;
    }
    else
    {
        return int.TryParse(location, System.Globalization.NumberStyles.AllowHexSpecifier, null, out address);
    }
}
