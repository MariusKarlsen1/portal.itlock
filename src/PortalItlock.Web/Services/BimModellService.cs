using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Prosjektets BIM-modell (IFC). Fila kan være flere hundre MB, så den lagres
// på disk ved siden av organisasjonens database (mappen «modeller/<database>»)
// i stedet for i databasen. Selve lesingen av modellen skjer i nettleseren
// (wwwroot/js/bimModell.js); her lagres fila, og dørene fra modellen
// sammenlignes med dørene i portalen og importeres.
// Bruker sin egen databasekontekst, så lagringen her ikke tar med
// ulagrede endringer fra siden den kalles fra (f.eks. prosjektskjemaet).
public partial class BimModellService(DbContextOptions<ApplicationDbContext> options)
{
    private ApplicationDbContext NyDb() => new(options);

    public const long MaksStorrelse = 500_000_000;

    // Én dør slik den står i modellen (fra bimModell.lesDorer).
    public sealed class IfcDor
    {
        public string Guid { get; set; } = "";
        public string? Tag { get; set; }
        public string? Navn { get; set; }
        public string? Etasje { get; set; }
        public int? Bredde { get; set; }
        public int? Hoyde { get; set; }
        public Dictionary<string, string> Egenskaper { get; set; } = [];
    }

    public sealed class IfcDorliste
    {
        public List<IfcDor> Dorer { get; set; } = [];
        public List<string> Etasjer { get; set; } = [];
    }

    public enum Forslagstype { Ny, Koblet, Endret }

    // Én rad i importen: døra fra modellen, døra i portalen den er koblet til
    // (samme GlobalId eller samme dørnummer) og hva som er forskjellig.
    public sealed class Forslag
    {
        public required IfcDor Fra { get; init; }
        public string? Nummer { get; init; }
        public string? Etasje { get; init; }
        public string? Brann { get; init; }
        public Dor? Dor { get; init; }
        public List<string> Endringer { get; init; } = [];
        public Forslagstype Type => Dor is null ? Forslagstype.Ny : Endringer.Count > 0 ? Forslagstype.Endret : Forslagstype.Koblet;
        public bool Valgt { get; set; }
    }

    public sealed record NummerKilde(string Kode, string Visning, int Treff, int Utfylt);

    // ---------- Fila ----------

    private string Mappe()
    {
        using var db = NyDb();
        var fil = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var full = Path.GetFullPath(string.IsNullOrWhiteSpace(fil) ? "portalitlock.db" : fil);
        return Path.Combine(Path.GetDirectoryName(full)!, "modeller", Path.GetFileNameWithoutExtension(full));
    }

    public string Sti(int prosjektId) => Path.Combine(Mappe(), $"prosjekt-{prosjektId}.ifc");

    public bool Finnes(int prosjektId) => File.Exists(Sti(prosjektId));

    public async Task LagreAsync(int prosjektId, Stream data, string filnavn, string? lastetOppAv, CancellationToken avbryt = default)
    {
        await using var db = NyDb();
        var prosjekt = await db.Prosjekter.FirstAsync(p => p.Id == prosjektId, avbryt);
        Directory.CreateDirectory(Mappe());
        var sti = Sti(prosjektId);
        var temp = sti + ".tmp";
        long storrelse;
        await using (var ut = File.Create(temp))
        {
            await data.CopyToAsync(ut, avbryt);
            storrelse = ut.Length;
        }

        // Enkel sjekk av at det er en IFC-fil (STEP-format) før den tas i bruk.
        using (var les = new StreamReader(temp))
        {
            var start = new char[64];
            var n = await les.ReadBlockAsync(start, 0, start.Length);
            if (!new string(start, 0, n).TrimStart().StartsWith("ISO-10303-21", StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(temp);
                throw new InvalidDataException("Fila er ikke en IFC-fil.");
            }
        }

        File.Move(temp, sti, overwrite: true);
        prosjekt.ModellFilnavn = Path.GetFileName(filnavn);
        prosjekt.ModellStorrelse = storrelse;
        prosjekt.ModellOpplastet = DateTime.Now;
        prosjekt.ModellOpplastetAv = lastetOppAv;
        await db.SaveChangesAsync(avbryt);
    }

    public async Task SlettAsync(int prosjektId)
    {
        await using var db = NyDb();
        var prosjekt = await db.Prosjekter.FirstAsync(p => p.Id == prosjektId);
        if (File.Exists(Sti(prosjektId)))
        {
            File.Delete(Sti(prosjektId));
        }

        prosjekt.ModellFilnavn = null;
        prosjekt.ModellStorrelse = null;
        prosjekt.ModellOpplastet = null;
        prosjekt.ModellOpplastetAv = null;
        await db.Dorer.Where(d => d.ProsjektId == prosjektId && d.IfcGuid != null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IfcGuid, (string?)null));
        await db.SaveChangesAsync();
    }

    // ---------- Sammenligning ----------

    // Hvilket felt i modellen som er dørnummeret varierer mellom programmene
    // (ArchiCAD: Tag, Revit: «Mark» i en egenskap, andre: navnet). Alle
    // kandidatene foreslås, sortert etter hvor mange som stemmer med dørene
    // som allerede er i portalen, og deretter hvor mange som er utfylt.
    public static List<NummerKilde> Nummerkilder(IReadOnlyList<IfcDor> fraModell, IEnumerable<string> eksisterende)
    {
        var finnes = eksisterende.Select(Normaliser).ToHashSet();
        var kilder = new List<(string Kode, string Visning)> { ("tag", "Tag (element-ID)"), ("navn", "Navn") };
        kilder.AddRange(fraModell.SelectMany(d => d.Egenskaper.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(k => NummerEgenskap().IsMatch(k))
            .Select(k => ($"egenskap:{k}", $"Egenskap «{k}»")));

        return kilder.Select(k =>
            {
                var verdier = fraModell.Select(d => Nummer(d, k.Kode)).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                return new NummerKilde(k.Kode, k.Visning, verdier.Count(v => finnes.Contains(Normaliser(v!))), verdier.Count);
            })
            .Where(k => k.Utfylt > 0)
            .OrderByDescending(k => k.Treff).ThenByDescending(k => k.Kode.StartsWith("egenskap:")).ThenByDescending(k => k.Kode == "tag").ThenByDescending(k => k.Utfylt)
            .ToList();
    }

    public static string? Nummer(IfcDor d, string kilde) => kilde switch
    {
        "tag" => Ren(d.Tag),
        "navn" => Ren(d.Navn),
        _ when kilde.StartsWith("egenskap:") => d.Egenskaper.FirstOrDefault(e => string.Equals(e.Key, kilde[9..], StringComparison.OrdinalIgnoreCase)).Value is { } v ? Ren(v) : null,
        _ => null
    };

    // Brannklassen står oftest i Pset_DoorCommon.FireRating.
    public static string? Brann(IfcDor d) =>
        Ren(d.Egenskaper.FirstOrDefault(e => e.Key.Equals("FireRating", StringComparison.OrdinalIgnoreCase)).Value
            ?? d.Egenskaper.FirstOrDefault(e => e.Key.Contains("brann", StringComparison.OrdinalIgnoreCase) || e.Key.Contains("fire", StringComparison.OrdinalIgnoreCase)).Value);

    // «Plan 01», «1. etasje», «Etasje 1», «Level 1» → «1». Andre navn (U1, Loft) beholdes.
    public static string? Etasje(string? navn)
    {
        if (string.IsNullOrWhiteSpace(navn))
        {
            return null;
        }

        var m = EtasjeNavn().Match(navn.Trim());
        return m.Success ? int.Parse(m.Groups["n"].Value).ToString() : navn.Trim();
    }

    public static List<Forslag> Sammenlign(IReadOnlyList<IfcDor> fraModell, IReadOnlyList<Dor> dorer, string nummerkilde)
    {
        var etterGuid = dorer.Where(d => !string.IsNullOrWhiteSpace(d.IfcGuid)).GroupBy(d => d.IfcGuid!).ToDictionary(g => g.Key, g => g.First());
        var etterNummer = dorer.GroupBy(d => Normaliser(d.Dornummer)).ToDictionary(g => g.Key, g => g.First());
        var brukt = new HashSet<int>();
        var resultat = new List<Forslag>();

        foreach (var f in fraModell)
        {
            var nummer = Nummer(f, nummerkilde);
            var dor = etterGuid.GetValueOrDefault(f.Guid);
            if (dor is null && nummer is not null && etterNummer.TryGetValue(Normaliser(nummer), out var medNummer) && !brukt.Contains(medNummer.Id)
                && (string.IsNullOrWhiteSpace(medNummer.IfcGuid) || medNummer.IfcGuid == f.Guid))
            {
                dor = medNummer;
            }

            if (dor is not null)
            {
                brukt.Add(dor.Id);
            }

            var etasje = Etasje(f.Etasje);
            var brann = Brann(f);
            var endringer = new List<string>();
            if (dor is not null)
            {
                if (f.Bredde is { } b && dor.Bredde is { } db1 && Math.Abs(b - db1) > 5) endringer.Add($"Bredde {db1} → {b}");
                if (f.Hoyde is { } h && dor.Hoyde is { } dh && Math.Abs(h - dh) > 5) endringer.Add($"Høyde {dh} → {h}");
                if (brann is not null && !string.IsNullOrWhiteSpace(dor.Brann) && !string.Equals(brann, dor.Brann.Trim(), StringComparison.OrdinalIgnoreCase)) endringer.Add($"Brann {dor.Brann} → {brann}");
                if (etasje is not null && !string.IsNullOrWhiteSpace(dor.Etasje) && !string.Equals(etasje, dor.Etasje.Trim(), StringComparison.OrdinalIgnoreCase)) endringer.Add($"Etasje {dor.Etasje} → {etasje}");
            }

            resultat.Add(new Forslag
            {
                Fra = f,
                Nummer = nummer,
                Etasje = etasje,
                Brann = brann,
                Dor = dor,
                Endringer = endringer,
                // Nye dører med nummer importeres som standard; endringer må velges.
                Valgt = dor is null && nummer is not null
            });
        }

        return resultat
            .OrderBy(r => r.Type == Forslagstype.Koblet)
            .ThenBy(r => r.Etasje ?? "", StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Nummer ?? "", StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // Kobler alle treff til modellen (GlobalId), oppretter valgte nye dører
    // og oppdaterer valgte endringer. Tomme felt i portalen fylles alltid fra
    // modellen. Returnerer (nye, koblet, oppdatert).
    public async Task<(int Nye, int Koblet, int Oppdatert)> ImporterAsync(int prosjektId, IReadOnlyList<Forslag> forslag, int? brukerId)
    {
        await using var db = NyDb();
        int nye = 0, koblet = 0, oppdatert = 0;
        var dorer = await db.Dorer.Where(d => d.ProsjektId == prosjektId).ToDictionaryAsync(d => d.Id);
        var brukteNumre = dorer.Values.Select(d => Normaliser(d.Dornummer)).ToHashSet();

        foreach (var f in forslag)
        {
            if (f.Dor is { } fd && dorer.TryGetValue(fd.Id, out var dor))
            {
                if (dor.IfcGuid != f.Fra.Guid)
                {
                    dor.IfcGuid = f.Fra.Guid;
                    koblet++;
                }

                var overskriv = f.Valgt && f.Endringer.Count > 0;
                if (f.Fra.Bredde is { } b && (dor.Bredde is null || overskriv)) dor.Bredde = b;
                if (f.Fra.Hoyde is { } h && (dor.Hoyde is null || overskriv)) dor.Hoyde = h;
                if (f.Brann is { } br && (string.IsNullOrWhiteSpace(dor.Brann) || overskriv)) dor.Brann = br;
                if (f.Etasje is { } e && (string.IsNullOrWhiteSpace(dor.Etasje) || overskriv)) dor.Etasje = e;
                if (overskriv)
                {
                    oppdatert++;
                }

                if (db.Entry(dor).State == EntityState.Modified)
                {
                    dor.OppdatertDato = DateTime.Now;
                    dor.OppdatertAvBrukerId = brukerId;
                }
            }
            else if (f.Valgt && f.Nummer is { } nr && brukteNumre.Add(Normaliser(nr)))
            {
                db.Dorer.Add(new Dor
                {
                    ProsjektId = prosjektId,
                    Dornummer = nr,
                    IfcGuid = f.Fra.Guid,
                    Etasje = f.Etasje,
                    Bredde = f.Fra.Bredde,
                    Hoyde = f.Fra.Hoyde,
                    BxH = f.Fra.Bredde is { } bb && f.Fra.Hoyde is { } hh ? $"{bb}x{hh}" : null,
                    Brann = f.Brann,
                    OppdatertAvBrukerId = brukerId
                });
                nye++;
            }
        }

        await db.SaveChangesAsync();
        return (nye, koblet, oppdatert);
    }

    private static string? Ren(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Normaliser(string s) => Regex.Replace(s.Trim(), @"\s+", "").ToUpperInvariant();

    [GeneratedRegex(@"^(mark|merke|dørnummer|dornummer|dør ?nr|door ?number|number|nummer|nr|reference|referanse|id)$", RegexOptions.IgnoreCase)]
    private static partial Regex NummerEgenskap();

    [GeneratedRegex(@"^(?:(?:plan|etasje|etg\.?|level|storey|floor)\s*0*(?<n>\d{1,2})|0*(?<n>\d{1,2})\s*\.?\s*(?:etasje|etg\.?|plan|level|floor)?)$", RegexOptions.IgnoreCase)]
    private static partial Regex EtasjeNavn();
}
