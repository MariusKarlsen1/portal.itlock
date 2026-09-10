using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Garanti spores per installert komponent, ikke per dør: hvert beslag har sin egen
// garantitid (satt på produktet i komponentregisteret), men nedtellingen starter først
// når PROSJEKTET er overlevert (juridisk reklamasjonsstartspunkt) - ikke når det enkelte
// beslaget ble montert.
public static class GarantiBeregning
{
    public record GarantiLinje(string ComponentNavn, int Antall, DateTime? UtlopsDato);

    public static List<GarantiLinje> Linjer(Dor dor, DateTime? prosjektOverlevertDato) =>
        dor.Komponenter
            .Where(dk => dk.Component is not null)
            .Select(dk => new GarantiLinje(
                dk.Component!.Navn,
                dk.Antall,
                prosjektOverlevertDato.HasValue && dk.Component.GarantitidManeder.HasValue
                    ? prosjektOverlevertDato.Value.AddMonths(dk.Component.GarantitidManeder.Value)
                    : null))
            .ToList();

    // Konservativt samlet mål for døren: den komponenten som går ut av garanti først,
    // siden en reklamasjon på en dør ikke nødvendigvis kan knyttes til ett bestemt beslag.
    public static DateTime? TidligsteUtlop(Dor dor, DateTime? prosjektOverlevertDato) =>
        Linjer(dor, prosjektOverlevertDato)
            .Where(l => l.UtlopsDato.HasValue)
            .Select(l => l.UtlopsDato!.Value)
            .OrderBy(d => d)
            .Cast<DateTime?>()
            .FirstOrDefault();
}
