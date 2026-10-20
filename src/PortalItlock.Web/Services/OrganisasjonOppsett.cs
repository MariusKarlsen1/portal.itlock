using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Felles regler for oppsettet av en organisasjon: e-postadressen på
// full-kontroll.no og standardinnstillingene. Brukes når en organisasjon
// opprettes, ved oppstart (for organisasjoner som fantes fra før) og av
// fanen «E-post og varsler» i adminportalen.
public static class OrganisasjonOppsett
{
    public const string EpostDomene = "full-kontroll.no";

    // Adresser som ikke kan gis til en organisasjon.
    private static readonly HashSet<string> Reservert = new(StringComparer.OrdinalIgnoreCase)
    {
        "post", "admin", "administrator", "noreply", "no-reply", "support", "hjelp", "varsel", "varsler",
        "postmaster", "hostmaster", "abuse", "webmaster", "www", "mail", "info", "plattform", "full-kontroll"
    };

    public static string Adresse(string epostNavn) => $"{epostNavn}@{EpostDomene}";

    // Gjør et navn om til en gyldig e-postdel: små bokstaver, a-z, 0-9 og
    // bindestrek, æøå byttes ut.
    public static string Slug(string navn)
    {
        // Selskapsform på slutten tas bort: «Sørlandet Lås AS» blir sorlandet-las.
        var normalisert = System.Text.RegularExpressions.Regex.Replace(navn.Trim().ToLowerInvariant(), @"\s+(as|asa|ans|da|enk)$", "")
            .Replace("æ", "ae").Replace("ø", "o").Replace("å", "a");
        var tegn = normalisert.Select(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-').ToArray();
        var slug = new string(tegn);
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        slug = slug.Trim('-');
        if (slug.Length > 40)
        {
            slug = slug[..40].TrimEnd('-');
        }

        return slug.Length >= 2 ? slug : "organisasjon";
    }

    // Feilmelding hvis navnet ikke kan brukes, ellers null.
    public static string? SjekkEpostNavn(PlatformDbContext db, string epostNavn, int tenantId)
    {
        if (epostNavn.Length is < 2 or > 40)
        {
            return "Adressen må ha mellom 2 og 40 tegn.";
        }

        if (epostNavn != Slug(epostNavn))
        {
            return "Bruk bare små bokstaver a-z, tall og bindestrek.";
        }

        if (Reservert.Contains(epostNavn))
        {
            return $"{Adresse(epostNavn)} er reservert. Velg en annen.";
        }

        if (db.TenantInnstillinger.Any(i => i.EpostNavn == epostNavn && i.TenantId != tenantId))
        {
            return $"{Adresse(epostNavn)} brukes allerede av en annen organisasjon.";
        }

        return null;
    }

    // Sørger for at organisasjonen har innstillinger med en ledig adresse.
    // Lagrer ikke; kalleren gjør SaveChanges.
    public static TenantInnstillinger SikreInnstillinger(PlatformDbContext db, Tenant tenant)
    {
        var inn = db.TenantInnstillinger.Local.FirstOrDefault(i => i.TenantId == tenant.Id)
            ?? db.TenantInnstillinger.FirstOrDefault(i => i.TenantId == tenant.Id);
        if (inn is null)
        {
            inn = new TenantInnstillinger { TenantId = tenant.Id };
            db.TenantInnstillinger.Add(inn);
        }

        inn.AvsenderNavn ??= tenant.PortalVisningsnavn ?? tenant.Navn;

        if (string.IsNullOrWhiteSpace(inn.EpostNavn))
        {
            var basis = Slug(tenant.Navn);
            var forslag = basis;
            var nr = 2;
            while (SjekkEpostNavn(db, forslag, tenant.Id) is not null
                || db.TenantInnstillinger.Local.Any(i => i != inn && i.EpostNavn == forslag))
            {
                forslag = $"{basis}-{nr++}";
            }

            inn.EpostNavn = forslag;
        }

        return inn;
    }
}
