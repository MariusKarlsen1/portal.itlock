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

    // Sletter oppføringen. Returnerer en feiltekst hvis databasen nekter.
    public async Task<string?> SlettAsync(SletteType type, int id)
    {
        var funnet = await db.FindAsync(Entitet(type), id);
        if (funnet is null)
        {
            return null;
        }
        db.Remove(funnet);
        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException)
        {
            // Rull tilbake sporingen så resten av siden fortsatt virker.
            db.Entry(funnet).State = EntityState.Unchanged;
            return $"Klarte ikke å slette {Tittel(type)} fordi annen informasjon fortsatt peker på den. Fjern koblingene først, eller kontakt support.";
        }
    }
}
