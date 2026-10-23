using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Regner ut de samme linjene som sendes til Tripletex som ordrelinjer
// (se TripletexSyncService.PushArbeidsordreTilTripletexAsync), men som en
// ren, databaseuavhengig funksjon - slik at rapportene (RapportKunde/
// RapportProdukt/RapportResultat) kan vise akkurat den samme omsetningen
// beregnet fra portalens egne data, uten å vente på eller spørre Tripletex.
public static class ArbeidsordreOkonomiBeregner
{
    // Varenummer er priskoden (= varenummeret i Tripletex) på kodelinjene.
    public sealed record OkonomiLinje(
        string Navn,
        int? ComponentId,
        int? KontoNummer,
        string? KontoNavn,
        decimal Antall,
        decimal Belop,
        string? Varenummer = null);

    // Krever at ordre er lastet med Tilbud.Linjer.Component.Inntektskonto og
    // Varer.Component.Inntektskonto. Med kodelinjer også Tilbud.KodeLinjer,
    // KodeLinjer og Timeregistreringer, og priskodene i koder.
    public static List<OkonomiLinje> BeregnLinjer(Arbeidsordre ordre, IReadOnlyList<PrisKode>? koder = null)
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

            if (ordre.Tilbud.BrukerKodelinjer && ordre.Tilbud.KodeLinjer.Count > 0)
            {
                // Fastpris fra tilbudet: én linje per kode, antall 1 og summen.
                linjer.AddRange(TilbudKoder(ordre.Tilbud));
            }
            else
            {
                var minutter = ordre.Tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F).Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
                var arbeidstidTimer = ordre.Tilbud.EstimertTimerOverride ?? (minutter / 60m);
                var kalkulertMontasjekost = Math.Round(ordre.Tilbud.Timepris * arbeidstidTimer, 2);
                var montasjekost = ordre.Tilbud.Montasjekost ?? kalkulertMontasjekost;
                if (montasjekost > 0)
                {
                    linjer.Add(new OkonomiLinje("Montasje", null, null, null, 1, montasjekost));
                }
            }
        }
        else
        {
            if (ordre.Timepris is not null && ordre.EstimerteTimer is not null)
            {
                linjer.Add(new OkonomiLinje("Montasje/arbeid", null, null, null, 1, ordre.Timepris.Value * ordre.EstimerteTimer.Value));
            }
            else if (koder is { Count: > 0 })
            {
                // Uten fastpris: de fakturerbare timene på hver arbeidskode.
                linjer.AddRange(Arbeid(ordre, koder));
            }

            // Kjøring og tillegg montøren har ført (på fastpris er de bare kost).
            linjer.AddRange(ordre.KodeLinjer
                .GroupBy(k => (k.Kode, k.Navn))
                .Select(g => Linje(g.Key.Kode, g.Key.Navn, g.Sum(k => k.Antall * k.Salgspris)))
                .Where(l => l.Belop != 0));
        }

        foreach (var v in ordre.Varer)
        {
            linjer.Add(new OkonomiLinje(
                v.Navn, v.ComponentId, v.Component?.Inntektskonto?.Nummer, v.Component?.Inntektskonto?.Navn,
                v.Antall, v.Antall * v.Utpris));
        }

        return linjer;
    }

    private static OkonomiLinje Linje(string kode, string navn, decimal belop) =>
        new(navn, null, null, null, 1, Math.Round(belop, 2), kode);

    private static IEnumerable<OkonomiLinje> TilbudKoder(Tilbud tilbud)
    {
        var utprisVarer = tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering).Sum(l => l.Utpris * l.Antall);
        return tilbud.KodeLinjer.OrderBy(l => l.Rekkefolge).ThenBy(l => l.Id)
            .Select(l => Linje(l.Kode, l.Navn, KodeLinjeBeregner.Sum(l, tilbud.KodeLinjer, utprisVarer).Salg))
            .Where(l => l.Belop != 0);
    }

    // Arbeidskoden timene føres på når ingen er valgt: montasjekoden.
    public static PrisKode? Montasjekode(IReadOnlyList<PrisKode> koder) =>
        koder.Where(k => k.Type == PrisKodeType.Arbeid && k.Enhet == PrisKodeEnhet.Time)
            .OrderByDescending(k => k.ErMontasje).ThenByDescending(k => k.Aktiv).ThenBy(k => k.Rekkefolge).FirstOrDefault();

    private static IEnumerable<Timeregistrering> Fakturerbare(Arbeidsordre ordre) =>
        ordre.Timeregistreringer.Where(t => t.Fakturerbar && t.Status != TimeregistreringStatus.Avslatt);

    private static IEnumerable<OkonomiLinje> Arbeid(Arbeidsordre ordre, IReadOnlyList<PrisKode> koder)
    {
        var standard = Montasjekode(koder);
        return Fakturerbare(ordre)
            .GroupBy(t => koder.FirstOrDefault(k => k.Id == t.PrisKodeId && k.Type == PrisKodeType.Arbeid) ?? standard)
            .Where(g => g.Key is not null)
            .Select(g => Linje(g.Key!.Kode, g.Key.Navn, g.Sum(t => t.TotalTimer) * g.Key.Salgspris))
            .Where(l => l.Belop != 0);
    }

    // Faktisk kost for kjøring og tillegg montøren har ført på arbeidsordren.
    public static decimal AndreKost(IEnumerable<ArbeidsordreKodeLinje> kodeLinjer) =>
        Math.Round(kodeLinjer.Sum(l => l.Antall * l.Kostpris), 2);
}
