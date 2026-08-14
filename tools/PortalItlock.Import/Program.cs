using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

if (args[0] == "pakker")
{
    if (args.Length < 3) { PrintUsage(); return 1; }
    return await RunPackageImport(args[1], args[2]);
}

if (args[0] == "priser")
{
    if (args.Length < 3) { PrintUsage(); return 1; }
    return await RunPriceImport(args[1], args[2]);
}

PrintUsage();
return 1;

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  PortalItlock.Import pakker <dumpTsvPath> <sqliteDbPath>");
    Console.WriteLine("  PortalItlock.Import priser <priserFolderPath> <sqliteDbPath>");
}

static async Task<int> RunPackageImport(string tsvPath, string dbPath)
{
    var lines = File.ReadAllLines(tsvPath);
    // lines[0] = title row (e.g. "R2"), lines[1] = header row ("R3\t<84 headers>"), lines[2..] = data rows.
    var headerCells = lines[1].Split('\t').Skip(1).ToArray();

    var dataRows = lines.Skip(2)
        .Select(l => l.Split('\t'))
        .Where(cells => cells.Length > 10 && cells.Skip(1).Any(c => c.Trim().Length > 0))
        .ToList();

    Console.WriteLine($"Parsed {dataRows.Count} data rows, {headerCells.Length} columns.");

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    using var db = new ApplicationDbContext(options);

    var dimensions = await db.RequirementDimensions
        .Include(d => d.Verdier)
        .OrderBy(d => d.Rekkefolge)
        .ToListAsync();

    if (dimensions.Count != 9)
    {
        Console.WriteLine($"Expected 9 requirement dimensions, found {dimensions.Count}. Aborting.");
        return 1;
    }

    var componentTypes = await db.ComponentTypes.OrderBy(t => t.Id).ToListAsync();
    if (componentTypes.Count != 65)
    {
        Console.WriteLine($"Expected 65 component types, found {componentTypes.Count}. Aborting.");
        return 1;
    }

    var valueLookup = new Dictionary<(int dimIdx, string verdi), RequirementValue>();
    for (int i = 0; i < 9; i++)
    {
        foreach (var v in dimensions[i].Verdier)
        {
            valueLookup[(i, v.Verdi)] = v;
        }
    }

    var componentCache = new Dictionary<(int typeId, string navn), Component>();

    const int ValgStart = 0;      // columns 0-8
    const int ResultatCol = 9;
    const int ComponentStart = 10; // columns 10-74 (65 slots)
    const int ComponentEnd = 74;
    const int MerknadStart = 76;   // columns 76-83 (8 notes), 75 is a redundant calculated concat column
    const int MerknadEnd = 83;

    string Cell(string[] cells, int col) => (col + 1) < cells.Length ? cells[col + 1].Trim() : "";

    int imported = 0, skipped = 0;

    foreach (var cells in dataRows)
    {
        var navn = Cell(cells, ResultatCol);
        if (navn.Length == 0)
        {
            skipped++;
            continue;
        }

        var package = new Package
        {
            Navn = navn,
            ErManuell = false
        };

        for (int dimIdx = 0; dimIdx < 9; dimIdx++)
        {
            var verdiText = Cell(cells, ValgStart + dimIdx);
            if (verdiText.Length == 0)
            {
                continue;
            }

            if (!valueLookup.TryGetValue((dimIdx, verdiText), out var value))
            {
                Console.WriteLine($"WARN: package '{navn}' - no requirement value match for dimension {dimensions[dimIdx].Navn} = '{verdiText}'");
                continue;
            }

            package.Krav.Add(new PackageRequirement { RequirementValue = value });
        }

        for (int col = ComponentStart; col <= ComponentEnd; col++)
        {
            var text = Cell(cells, col);
            if (text.Length == 0)
            {
                continue;
            }

            var typeId = componentTypes[col - ComponentStart].Id;
            var key = (typeId, text);

            if (!componentCache.TryGetValue(key, out var component))
            {
                component = new Component
                {
                    ComponentTypeId = typeId,
                    Navn = text
                };
                componentCache[key] = component;
                db.Components.Add(component);
            }

            package.Komponenter.Add(new PackageComponent { Component = component, Antall = 1 });
        }

        var notes = new List<string>();
        for (int col = MerknadStart; col <= MerknadEnd; col++)
        {
            var text = Cell(cells, col);
            if (text.Length > 0)
            {
                notes.Add($"{headerCells[col]}: {text}");
            }
        }
        if (notes.Count > 0)
        {
            package.Beskrivelse = string.Join("\n", notes);
        }

        db.Packages.Add(package);
        imported++;
    }

    await db.SaveChangesAsync();

    Console.WriteLine($"Imported {imported} packages, skipped {skipped} rows without a name.");
    Console.WriteLine($"Created {componentCache.Count} distinct components.");
    return 0;
}

static async Task<int> RunPriceImport(string priserFolder, string dbPath)
{
    var specs = new List<PriceFileSpec>
    {
        new("boyesen/dump_sheet1.xml.tsv", "Boyesen & Munthe", 2,
            Varenummer: 0, Varenavn: 1, PrisVeiledende: 2, PrisNetto: null, Varegruppe: null),
        new("bfs/dump_sheet1.xml.tsv", "BFS", 5,
            Varenummer: 1, Varenavn: 2, PrisVeiledende: 4, PrisNetto: 6, Varegruppe: 0),
        new("iloq/dump_sheet1.xml.tsv", "ILOQ", 2,
            Varenummer: 0, Varenavn: 1, PrisVeiledende: 3, PrisNetto: 4, Varegruppe: null),
        new("dormakaba/dump_sheet1.xml.tsv", "Dormakaba", 2,
            Varenummer: 0, Varenavn: 2, PrisVeiledende: 6, PrisNetto: null, Varegruppe: 4),
        new("steplock/dump_sheet1.xml.tsv", "Steplock Norway", 7,
            Varenummer: 0, Varenavn: 1, PrisVeiledende: 3, PrisNetto: 4, Varegruppe: null),
        new("steplock/dump_sheet2.xml.tsv", "Steplock Norway", 7,
            Varenummer: 0, Varenavn: 1, PrisVeiledende: 2, PrisNetto: 3, Varegruppe: null),
        new("salto/dump_sheet1.xml.tsv", "SALTO Systems", 13,
            Varenummer: 1, Varenavn: 0, PrisVeiledende: 4, PrisNetto: 7, Varegruppe: null),
    };

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    using var db = new ApplicationDbContext(options);

    int totalImported = 0, totalSkipped = 0;

    foreach (var spec in specs)
    {
        var path = Path.Combine(priserFolder, spec.RelativePath);
        if (!File.Exists(path))
        {
            Console.WriteLine($"WARN: missing {path}, skipping.");
            continue;
        }

        var rows = ParseRows(path);
        int imported = 0, skipped = 0;

        foreach (var cells in rows)
        {
            if (RowNumber(cells) < spec.FirstDataRow)
            {
                continue;
            }

            var navn = Cell(cells, spec.Varenavn);
            if (navn.Length == 0)
            {
                skipped++;
                continue;
            }

            var varenummer = spec.Varenummer.HasValue ? Cell(cells, spec.Varenummer.Value) : "";
            var varegruppe = spec.Varegruppe.HasValue ? Cell(cells, spec.Varegruppe.Value) : "";

            var component = new Component
            {
                Navn = navn,
                Leverandor = spec.Leverandor,
                Produktkode = varenummer.Length > 0 ? varenummer : null,
                Varegruppe = varegruppe.Length > 0 ? varegruppe : null,
                PrisVeiledende = spec.PrisVeiledende.HasValue ? ParsePrice(Cell(cells, spec.PrisVeiledende.Value)) : null,
                PrisNetto = spec.PrisNetto.HasValue ? ParsePrice(Cell(cells, spec.PrisNetto.Value)) : null,
                ComponentTypeId = null,
                Aktiv = true
            };

            db.Components.Add(component);
            imported++;
        }

        Console.WriteLine($"{spec.Leverandor} ({spec.RelativePath}): imported {imported}, skipped {skipped}.");
        totalImported += imported;
        totalSkipped += skipped;
    }

    await db.SaveChangesAsync();

    Console.WriteLine($"TOTAL: imported {totalImported} components, skipped {totalSkipped} blank rows.");
    return 0;
}

static List<string[]> ParseRows(string path) =>
    File.ReadAllLines(path).Select(l => l.Split('\t')).ToList();

static int RowNumber(string[] cells) =>
    int.TryParse(cells[0].TrimStart('R'), out var n) ? n : 0;

static string Cell(string[] cells, int col) =>
    (col + 1) < cells.Length ? cells[col + 1].Trim() : "";

static decimal? ParsePrice(string text)
{
    if (text.Length == 0)
    {
        return null;
    }

    return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
        ? Math.Round(value, 2)
        : null;
}

record PriceFileSpec(
    string RelativePath,
    string Leverandor,
    int FirstDataRow,
    int? Varenummer,
    int Varenavn,
    int? PrisVeiledende,
    int? PrisNetto,
    int? Varegruppe);
