using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public enum SletteType
{
    Koblingsskjema,
    Arbeidsordre,
    Prosjekt,
    Serviceavtale,
    Ticket,
    Tilbud,
    Kunde,
}

// Hva som skjer med det som henger sammen med oppføringen når den slettes.
public sealed record SletteKonsekvens(List<string> SlettesOgsa, List<string> MisterKobling);

// Felles sletting fra listene (⋯-menyen). Konsekvensene leses fra datamodellen
// (fremmednøklene og deres sletteregel), så teksten i bekreftelsen alltid
// stemmer med det databasen faktisk gjør.
public class SletteService(ApplicationDbContext db)
{
    public static string Tittel(SletteType type) => type switch
    {
        SletteType.Koblingsskjema => "koblingsskjemaet",
        SletteType.Arbeidsordre => "arbeidsordren",
        SletteType.Prosjekt => "prosjektet",
        SletteType.Serviceavtale => "serviceavtalen",
        SletteType.Ticket => "ticketen",
        SletteType.Tilbud => "tilbudet",
        SletteType.Kunde => "kunden",
        _ => "oppføringen",
    };

    private static Type Entitet(SletteType type) => type switch
    {
        SletteType.Koblingsskjema => typeof(KoblingsSkjema),
        SletteType.Arbeidsordre => typeof(Arbeidsordre),
        SletteType.Prosjekt or SletteType.Serviceavtale => typeof(Prosjekt),
        SletteType.Ticket => typeof(Ticket),
        SletteType.Tilbud => typeof(Tilbud),
        SletteType.Kunde => typeof(Kunde),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    // Lesbare navn i flertall for det som teller i bekreftelsen. Typer som ikke
    // står her (interne koblingstabeller o.l.) vises ikke.
    private static readonly Dictionary<string, (string En, string Flere)> Navn = new()
    {
        ["Dor"] = ("dør", "dører"),
        ["Plantegning"] = ("plantegning", "plantegninger"),
        ["Tilbud"] = ("tilbud", "tilbud"),
        ["Arbeidsordre"] = ("arbeidsordre", "arbeidsordre"),
        ["Servicerunde"] = ("servicerunde", "servicerunder"),
        ["Avvik"] = ("avvik", "avvik"),
        ["LagretPdf"] = ("lagret PDF", "lagrede PDF-er"),
        ["Timeregistrering"] = ("timeføring", "timeføringer"),
        ["KoblingsSymbol"] = ("komponent", "komponenter"),
        ["KoblingsStrek"] = ("strek eller kabel", "streker og kabler"),
        ["KoblingsSkjema"] = ("koblingsskjema", "koblingsskjemaer"),
        ["Ticket"] = ("ticket", "tickets"),
        ["Prosjekt"] = ("prosjekt", "prosjekter"),
        ["Ressursplan"] = ("hendelse i kalenderen", "hendelser i kalenderen"),
        ["ProsjektVedlegg"] = ("vedlegg", "vedlegg"),
        ["ArbeidsordreMedia"] = ("bilde", "bilder"),
        ["ArbeidsordreVarer"] = ("varelinje", "varelinjer"),
        ["ArbeidsordreVare"] = ("varelinje", "varelinjer"),
        ["TilbudLinje"] = ("tilbudslinje", "tilbudslinjer"),
        ["TicketMedia"] = ("vedlegg", "vedlegg"),
        ["Foresporsel"] = ("forespørsel", "forespørsler"),
        ["KundeKontaktperson"] = ("kontaktperson", "kontaktpersoner"),
        ["KundeDokument"] = ("dokument", "dokumenter"),
        ["CeGodkjenning"] = ("CE-godkjenning", "CE-godkjenninger"),
        ["Driftsmelding"] = ("driftsmelding", "driftsmeldinger"),
        ["Innkjopsordre"] = ("innkjøpsordre", "innkjøpsordrer"),
    };

    public async Task<SletteKonsekvens> KonsekvensAsync(SletteType type, int id)
    {
        var slettes = new List<string>();
        var mister = new List<string>();
        var entitet = db.Model.FindEntityType(Entitet(type));
        if (entitet is null)
        {
            return new SletteKonsekvens(slettes, mister);
        }

        var tilkobling = db.Database.GetDbConnection();
        var apnetHer = tilkobling.State != System.Data.ConnectionState.Open;
        if (apnetHer)
        {
            await tilkobling.OpenAsync();
        }
        try
        {
            foreach (var fk in entitet.GetReferencingForeignKeys())
            {
                var avhengig = fk.DeclaringEntityType;
                if (!Navn.TryGetValue(avhengig.ClrType.Name, out var navn) || fk.Properties.Count != 1)
                {
                    continue;
                }
                var tabell = avhengig.GetTableName();
                var kolonne = fk.Properties[0].GetColumnName(StoreObjectIdentifier.Table(tabell!, avhengig.GetSchema()));
                if (tabell is null || kolonne is null)
                {
                    continue;
                }
                var antall = await TellAsync(tilkobling, tabell, kolonne, id);
                if (antall == 0)
                {
                    continue;
                }
                var tekst = $"{antall} {(antall == 1 ? navn.En : navn.Flere)}";
                if (fk.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    slettes.Add(tekst);
                }
                else
                {
                    mister.Add(tekst);
                }
            }
        }
        finally
        {
            if (apnetHer)
            {
                await tilkobling.CloseAsync();
            }
        }
        return new SletteKonsekvens(slettes.Distinct().ToList(), mister.Distinct().ToList());
    }

    private static async Task<long> TellAsync(DbConnection tilkobling, string tabell, string kolonne, int id)
    {
        await using var q = tilkobling.CreateCommand();
        q.CommandText = $"SELECT COUNT(*) FROM \"{tabell}\" WHERE \"{kolonne}\" = @id";
        var p = q.CreateParameter();
        p.ParameterName = "@id";
        p.Value = id;
        q.Parameters.Add(p);
        return Convert.ToInt64(await q.ExecuteScalarAsync() ?? 0);
    }

    // Sletter oppføringen. Returnerer en feiltekst hvis det ikke går.
    //
    // Databasen sletter selv alt som kaskaderer (dører, plantegninger, utstyr
    // osv.), men noen koblinger lenger ned i treet er låst (RESTRICT / NO ACTION),
    // f.eks. forbindelser mellom utstyr på en plantegning. Derfor leses
    // fremmednøklene fra databasen, og alt som ville stoppet slettingen ryddes
    // først: rader som uansett skal slettes slettes eksplisitt, andre mister
    // koblingen (settes til NULL). Alt skjer i én transaksjon.
    public async Task<string?> SlettAsync(SletteType type, int id)
    {
        var entitet = db.Model.FindEntityType(Entitet(type));
        var rotTabell = entitet?.GetTableName();
        if (entitet is null || rotTabell is null)
        {
            return null;
        }

        var tilkobling = db.Database.GetDbConnection();
        var apnetHer = tilkobling.State != System.Data.ConnectionState.Open;
        if (apnetHer)
        {
            await tilkobling.OpenAsync();
        }
        try
        {
            var (noekler, primaer) = await LesFremmednoklerAsync(tilkobling);
            var pk = primaer.GetValueOrDefault(rotTabell, "Id");
            if (await KjorAsync(tilkobling, null, $"SELECT COUNT(*) FROM \"{rotTabell}\" WHERE \"{pk}\" = @id", id, tell: true) == 0)
            {
                return null;
            }

            // Alle rader som slettes: tabell -> WHERE-uttrykk.
            var sett = new Dictionary<string, List<string>>();
            void Samle(string tabell, string hvor, int dybde)
            {
                if (!sett.TryGetValue(tabell, out var liste))
                {
                    sett[tabell] = liste = [];
                }
                liste.Add(hvor);
                if (dybde >= 8)
                {
                    return;
                }
                foreach (var fk in noekler.Where(f => f.Forelder == tabell && f.VedSletting == "CASCADE"))
                {
                    Samle(fk.Barn, $"\"{fk.Kolonne}\" IN (SELECT \"{fk.ForelderKolonne}\" FROM \"{tabell}\" WHERE {hvor})", dybde + 1);
                }
            }
            Samle(rotTabell, $"\"{pk}\" = @id", 0);

            var forhand = new List<string>();
            foreach (var (tabell, hvorListe) in sett)
            {
                foreach (var hvor in hvorListe)
                {
                    foreach (var fk in noekler.Where(f => f.Forelder == tabell && f.VedSletting is "NO ACTION" or "RESTRICT"))
                    {
                        var peker = $"\"{fk.Kolonne}\" IN (SELECT \"{fk.ForelderKolonne}\" FROM \"{tabell}\" WHERE {hvor})";
                        if (fk.Paakrevd)
                        {
                            // Raden kan ikke finnes uten det som slettes (f.eks. en
                            // forbindelse mellom to utstyr på en slettet plantegning).
                            forhand.Insert(0, $"DELETE FROM \"{fk.Barn}\" WHERE {peker}");
                            continue;
                        }
                        var resten = peker;
                        if (sett.TryGetValue(fk.Barn, out var barnHvor))
                        {
                            var iSettet = string.Join(" OR ", barnHvor.Select(h => $"({h})"));
                            forhand.Add($"DELETE FROM \"{fk.Barn}\" WHERE {peker} AND ({iSettet})");
                            resten = $"{peker} AND NOT ({iSettet})";
                        }
                        forhand.Add($"UPDATE \"{fk.Barn}\" SET \"{fk.Kolonne}\" = NULL WHERE {resten}");
                    }
                }
            }

            await using var tx = await tilkobling.BeginTransactionAsync();
            try
            {
                foreach (var sql in forhand)
                {
                    await KjorAsync(tilkobling, tx, sql, id);
                }
                await KjorAsync(tilkobling, tx, $"DELETE FROM \"{rotTabell}\" WHERE \"{pk}\" = @id", id);
                await tx.CommitAsync();
            }
            catch (DbException)
            {
                await tx.RollbackAsync();
                return $"Klarte ikke å slette {Tittel(type)} fordi annen informasjon fortsatt peker på den. Fjern koblingene først, eller kontakt support.";
            }
        }
        finally
        {
            if (apnetHer)
            {
                await tilkobling.CloseAsync();
            }
        }

        // Siden kan fortsatt ha oppføringen i minnet - glem den, så den ikke lagres på nytt.
        foreach (var e in db.ChangeTracker.Entries().Where(e => e.Metadata == entitet).ToList())
        {
            if (e.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue is int eid && eid == id)
            {
                e.State = EntityState.Detached;
            }
        }
        return null;
    }

    private sealed record Fremmednokkel(string Barn, string Kolonne, string Forelder, string ForelderKolonne, string VedSletting, bool Paakrevd);

    // Fremmednøklene slik databasen faktisk håndhever dem (SQLite).
    private static async Task<(List<Fremmednokkel> Noekler, Dictionary<string, string> Primaer)> LesFremmednoklerAsync(DbConnection c)
    {
        var tabeller = new List<string>();
        await using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'";
            await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                tabeller.Add(r.GetString(0));
            }
        }

        var primaer = new Dictionary<string, string>();
        var paakrevd = new Dictionary<(string, string), bool>();
        foreach (var t in tabeller)
        {
            await using var q = c.CreateCommand();
            q.CommandText = $"PRAGMA table_info(\"{t}\")";
            await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var kol = r.GetString(1);
                paakrevd[(t, kol)] = r.GetInt64(3) == 1;
                if (r.GetInt64(5) == 1 && !primaer.ContainsKey(t))
                {
                    primaer[t] = kol;
                }
            }
        }

        var noekler = new List<Fremmednokkel>();
        foreach (var t in tabeller)
        {
            await using var q = c.CreateCommand();
            q.CommandText = $"PRAGMA foreign_key_list(\"{t}\")";
            await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var forelder = r.GetString(2);
                var fra = r.GetString(3);
                var til = r.IsDBNull(4) ? primaer.GetValueOrDefault(forelder, "Id") : r.GetString(4);
                noekler.Add(new Fremmednokkel(t, fra, forelder, til, r.GetString(6).ToUpperInvariant(), paakrevd.GetValueOrDefault((t, fra))));
            }
        }
        return (noekler, primaer);
    }

    private static async Task<long> KjorAsync(DbConnection c, DbTransaction? tx, string sql, int id, bool tell = false)
    {
        await using var q = c.CreateCommand();
        q.Transaction = tx;
        q.CommandText = sql;
        var p = q.CreateParameter();
        p.ParameterName = "@id";
        p.Value = id;
        q.Parameters.Add(p);
        return tell ? Convert.ToInt64(await q.ExecuteScalarAsync() ?? 0) : await q.ExecuteNonQueryAsync();
    }
}
