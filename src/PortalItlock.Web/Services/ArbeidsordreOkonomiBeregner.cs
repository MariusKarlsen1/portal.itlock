using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Regner ut de samme linjene som sendes til Tripletex som ordrelinjer
// (se TripletexSyncService.PushArbeidsordreTilTripletexAsync), men som en
// ren, databaseuavhengig funksjon - slik at rapportene (RapportKunde/
// RapportProdukt/RapportResultat) kan vise akkurat den samme omsetningen
// beregnet fra portalens egne data, uten å vente på eller spørre Tripletex.
public static class ArbeidsordreOkonomiBeregner
{
    public sealed record OkonomiLinje(
        string Navn,
        int? ComponentId,
        int? KontoNummer,
        string? KontoNavn,
        decimal Antall,
        decimal Belop);

    // Krever at ordre er lastet med Tilbud.Linjer.Component.Inntektskonto og
    // Varer.Component.Inntektskonto.
    public static List<OkonomiLinje> BeregnLinjer(Arbeidsordre ordre)
    {
        var linjer = new List<OkonomiLinje>();

        if (ordre.Tilbud is not null)
        {
            var tilbudLinjer = ordre.Tilbud.Linjer
                .Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering)
                .OrderBy(l => l.Rekkefolge);
            foreach (var l in tilbudLinjer)
            {
                linjer.Add(new OkonomiLinje(
                    l.Navn, l.ComponentId, l.Component?.Inntektskonto?.Nummer, l.Component?.Inntektskonto?.Navn,
                    l.Antall, l.Antall * l.Utpris));
            }

            var minutter = ordre.Tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F).Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
            var arbeidstidTimer = ordre.Tilbud.EstimertTimerOverride ?? (minutter / 60m);
            var kalkulertMontasjekost = Math.Round(ordre.Tilbud.Timepris * arbeidstidTimer, 2);
            var montasjekost = ordre.Tilbud.Montasjekost ?? kalkulertMontasjekost;
            if (montasjekost > 0)
            {
                linjer.Add(new OkonomiLinje("Montasje", null, null, null, 1, montasjekost));
            }
        }
        else if (ordre.Timepris is not null && ordre.EstimerteTimer is not null)
        {
            linjer.Add(new OkonomiLinje("Montasje/arbeid", null, null, null, 1, ordre.Timepris.Value * ordre.EstimerteTimer.Value));
        }

        foreach (var v in ordre.Varer)
        {
            linjer.Add(new OkonomiLinje(
                v.Navn, v.ComponentId, v.Component?.Inntektskonto?.Nummer, v.Component?.Inntektskonto?.Navn,
                v.Antall, v.Antall * v.Utpris));
        }

        return linjer;
    }
}
