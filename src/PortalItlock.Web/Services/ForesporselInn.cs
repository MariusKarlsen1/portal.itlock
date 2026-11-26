using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Hjelp for e-post som kommer inn i Forespørsler:
//  - videresendt e-post (fra Outlook, Gmail osv.): finner den opprinnelige avsenderen
//    i «Fra:»/«From:»-linjen, så kunden står som avsender i stedet for den som videresendte
//  - automatiske svar («Fraværende», «Automatic reply») hoppes over
//  - kunden finnes fra e-postadressen, en kontaktperson eller domenet
public static partial class ForesporselInn
{
    public sealed record Avsender(string Epost, string? Navn);

    [GeneratedRegex(@"^\s*((fwd?|vs|wg|tr|fw)\s*:\s*)+", RegexOptions.IgnoreCase)]
    private static partial Regex VideresendPrefiks();

    // Markører som e-postprogrammene setter over den videresendte meldingen.
    [GeneratedRegex(@"(-{2,}\s*(forwarded message|videresendt melding|original message|opprinnelig melding|weitergeleitete nachricht)\s*-{2,})|(begin forwarded message)|(start på videresendt melding)", RegexOptions.IgnoreCase)]
    private static partial Regex VideresendMarkor();

    // «Fra: Kari Hansen <kari@x.no>», «From: kari@x.no», «*From:* Kari [mailto:kari@x.no]».
    [GeneratedRegex(@"^[\s>*]*(fra|from|von|avsender)\s*:\s*\**\s*(?<verdi>.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex FraLinje();

    [GeneratedRegex(@"[A-Z0-9._%+\-']+@[A-Z0-9.\-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EpostAdresse();

    [GeneratedRegex(@"^\s*(automatisk svar|autosvar|automatic reply|auto(matic)?[- ]?reply|out of (the )?office|fraværende|fravær:|abwesend|ikke til stede)", RegexOptions.IgnoreCase)]
    private static partial Regex AutosvarEmne();

    public static bool ErVideresendtEmne(string? emne) => !string.IsNullOrWhiteSpace(emne) && VideresendPrefiks().IsMatch(emne);

    // Emnet uten «VS:», «FW:», «Fwd:» foran.
    public static string RensEmne(string? emne)
    {
        var rent = VideresendPrefiks().Replace(emne ?? "", "").Trim();
        return rent.Length == 0 ? (emne ?? "").Trim() : rent;
    }

    // Den opprinnelige avsenderen hvis e-posten er videresendt, ellers null.
    // Krever enten «VS:/FW:/Fwd:» i emnet eller en videresendt-markør i teksten, og
    // tar første «Fra:»-linje etter markøren (eller i teksten om markøren mangler).
    public static Avsender? FinnOpprinneligAvsender(string? emne, string? tekst, string? videresendtAvEpost)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return null;
        }

        var markor = VideresendMarkor().Match(tekst);
        if (!markor.Success && !ErVideresendtEmne(emne))
        {
            return null;
        }

        var start = markor.Success ? markor.Index : 0;
        foreach (Match m in FraLinje().Matches(tekst[start..]))
        {
            var avsender = TolkAvsender(m.Groups["verdi"].Value);
            if (avsender is null)
            {
                continue;
            }
            // Den som videresendte, står ofte øverst i sin egen signatur/topptekst.
            if (!string.IsNullOrWhiteSpace(videresendtAvEpost)
                && string.Equals(avsender.Epost, videresendtAvEpost.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            return avsender;
        }

        return null;
    }

    // «Kari Hansen <kari@x.no>», «"Kari Hansen" [mailto:kari@x.no]», «kari@x.no».
    public static Avsender? TolkAvsender(string verdi)
    {
        var epost = EpostAdresse().Match(verdi);
        if (!epost.Success)
        {
            return null;
        }

        var navn = verdi[..epost.Index]
            .Replace("mailto:", "", StringComparison.OrdinalIgnoreCase)
            .Trim(' ', '"', '\'', '<', '[', '(', '*', '\t');
        return new Avsender(epost.Value.ToLowerInvariant(), navn.Length == 0 ? null : navn);
    }

    public static bool ErAutosvar(string? emne, string? rawJson)
    {
        if (AutosvarEmne().IsMatch(emne ?? ""))
        {
            return true;
        }
        // Headeren Auto-Submitted: auto-replied settes av e-postserveren på fraværsmeldinger.
        return rawJson is not null
            && rawJson.Contains("auto-submitted", StringComparison.OrdinalIgnoreCase)
            && rawJson.Contains("auto-replied", StringComparison.OrdinalIgnoreCase);
    }

    // Domener mange privatpersoner deler: her matches bare hele adressen, ikke domenet.
    private static readonly HashSet<string> FellesDomener = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "hotmail.com", "hotmail.no", "outlook.com", "outlook.no", "live.com", "live.no",
        "msn.com", "icloud.com", "me.com", "mac.com", "yahoo.com", "yahoo.no", "online.no", "frisurf.no", "start.no",
        "getmail.no", "broadpark.no", "lyse.net", "altibox.no", "c2i.net", "netcom.no", "telenor.no", "proton.me", "protonmail.com"
    };

    // Kunden til en e-postadresse: kundens egen e-post, så en kontaktperson, så
    // firmadomenet hvis nøyaktig én kunde har det.
    public static async Task<int?> FinnKundeAsync(ApplicationDbContext db, string? epost)
    {
        if (string.IsNullOrWhiteSpace(epost) || !epost.Contains('@'))
        {
            return null;
        }

        var e = epost.Trim().ToLowerInvariant();
        var kunde = await db.Kunder.AsNoTracking()
            .Where(k => k.Epost != null && k.Epost.ToLower() == e)
            .Select(k => (int?)k.Id).FirstOrDefaultAsync();
        if (kunde is not null)
        {
            return kunde;
        }

        kunde = await db.KundeKontaktpersoner.AsNoTracking()
            .Where(k => k.Epost != null && k.Epost.ToLower() == e)
            .Select(k => (int?)k.KundeId).FirstOrDefaultAsync();
        if (kunde is not null)
        {
            return kunde;
        }

        var domene = e[(e.LastIndexOf('@') + 1)..];
        if (FellesDomener.Contains(domene))
        {
            return null;
        }

        var slutt = "@" + domene;
        var treff = await db.Kunder.AsNoTracking()
            .Where(k => k.Epost != null && k.Epost.ToLower().EndsWith(slutt))
            .Select(k => k.Id)
            .Union(db.KundeKontaktpersoner.AsNoTracking()
                .Where(k => k.Epost != null && k.Epost.ToLower().EndsWith(slutt))
                .Select(k => k.KundeId))
            .Distinct()
            .Take(2)
            .ToListAsync();
        return treff.Count == 1 ? treff[0] : null;
    }
}
