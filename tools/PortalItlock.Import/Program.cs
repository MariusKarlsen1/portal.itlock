using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

if (args.Length < 2)
{
    Console.WriteLine("Usage: PortalItlock.Import <dumpTsvPath> <sqliteDbPath>");
    return 1;
}

var tsvPath = args[0];
var dbPath = args[1];

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
