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
        string? Varenummer = null,
        // Bare for Tripletex-ordren: innkjøpspris og veiledende pris per stk, og
        // produktkoden som står foran navnet (som når ordren legges inn for hånd).
        decimal? Innpris = null,
        decimal? Listepris = null,
        string? Produktkode = null);

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
                    l.Antall, l.Antall * l.Utpris,
                    Innpris: l.Innpris, Listepris: l.PrisVeiledende ?? l.Component?.PrisVeiledende, Produktkode: l.Component?.Produktkode));
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
        else if (ordre.ErDirekte)
        {
            // Direkte på ordren: arbeidet etter førte timer, eller avtalt sum.
            linjer.AddRange(DirekteArbeid(ordre, koder ?? []));
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

        // Varene først, så arbeidet (på direkte ordrer ligger arbeidet sist).
        var varer = ordre.Varer.Select(v => new OkonomiLinje(
                v.Navn, v.ComponentId, v.Component?.Inntektskonto?.Nummer, v.Component?.Inntektskonto?.Navn,
                v.Antall, v.Antall * v.Utpris,
                Innpris: v.Kostpris, Listepris: v.Listepris ?? v.Component?.PrisVeiledende, Produktkode: v.Component?.Produktkode))
            .ToList();
        if (ordre.ErDirekte && ordre.Tilbud is null)
        {
            linjer.InsertRange(0, varer);
        }
        else
        {
            linjer.AddRange(varer);
        }

        return linjer;
    }

    // Overstyring av én arbeidslinje på en ordre direkte på ordren (per kode).
    // Null = som ført / som på priskoden.
    public sealed class ArbeidOverstyring
    {
        public decimal? Antall { get; set; }
        public decimal? Kostpris { get; set; }
        public decimal? Utpris { get; set; }
        public decimal Rabatt { get; set; }
        public bool ErTom => Antall is null && Kostpris is null && Utpris is null && Rabatt == 0;
    }

    public static Dictionary<string, ArbeidOverstyring> LesOverstyring(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ArbeidOverstyring>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public static string? SkrivOverstyring(Dictionary<string, ArbeidOverstyring> o)
    {
        var med = o.Where(x => !x.Value.ErTom).ToDictionary(x => x.Key, x => x.Value);
        return med.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(med);
    }

    // Én arbeidslinje på en ordre direkte på ordren: timer per arbeidskode og
    // kjøring/tillegg montøren har ført, med eventuell overstyring.
    public sealed record ArbeidLinje(string Kode, string Navn, decimal FortAntall, decimal Antall, decimal Kostpris, decimal Utpris, decimal Rabatt, bool Overstyrt, bool ErTimer)
    {
        public decimal Sum => Math.Round(Antall * Utpris * (1 - Rabatt / 100m), 2);
        public decimal Kost => Math.Round(Antall * Kostpris, 2);
    }

    public static List<ArbeidLinje> DirekteArbeidLinjer(Arbeidsordre ordre, IReadOnlyList<PrisKode> koder)
    {
        var standard = Montasjekode(koder);
        var auto = new List<(string Kode, string Navn, decimal Antall, decimal Kost, decimal Ut, bool Timer)>();
        foreach (var g in Fakturerbare(ordre)
            .GroupBy(t => koder.FirstOrDefault(k => k.Id == t.PrisKodeId && k.Type == PrisKodeType.Arbeid) ?? standard)
            .Where(g => g.Key is not null))
        {
            var ut = ordre.Timepris is { } tp && g.Key!.Id == standard?.Id ? tp : g.Key!.Salgspris;
            auto.Add((g.Key.Kode, g.Key.Navn, Math.Round(g.Sum(t => t.TotalTimer), 2), g.Key.Kostpris, ut, true));
        }
        foreach (var g in ordre.KodeLinjer.GroupBy(k => (k.Kode, k.Navn)))
        {
            var antall = g.Sum(k => k.Antall);
            auto.Add((g.Key.Kode, g.Key.Navn, antall,
                antall == 0 ? 0 : Math.Round(g.Sum(k => k.Antall * k.Kostpris) / antall, 2),
                antall == 0 ? 0 : Math.Round(g.Sum(k => k.Antall * k.Salgspris) / antall, 2), false));
        }

        var over = LesOverstyring(ordre.ArbeidOverstyringJson);
        return auto
            .GroupBy(a => a.Kode)
            .Select(g =>
            {
                var f = g.First();
                var antall = g.Sum(x => x.Antall);
                over.TryGetValue(g.Key, out var o);
                return new ArbeidLinje(g.Key, f.Navn, antall, o?.Antall ?? antall, o?.Kostpris ?? f.Kost, o?.Utpris ?? f.Ut,
                    o?.Rabatt ?? 0, o is not null && !o.ErTom, f.Timer);
            })
            .ToList();
    }

    // Arbeidet på en ordre direkte på ordren som fakturalinjer.
    public static List<OkonomiLinje> DirekteArbeid(Arbeidsordre ordre, IReadOnlyList<PrisKode> koder) =>
        DirekteArbeidLinjer(ordre, koder)
            .Where(l => l.Sum != 0)
            .Select(l => new OkonomiLinje(l.Navn, null, null, null, l.Antall, l.Sum, l.Kode, Innpris: l.Kostpris))
            .ToList();

    public static decimal DirekteArbeidKost(Arbeidsordre ordre, IReadOnlyList<PrisKode> koder) =>
        DirekteArbeidLinjer(ordre, koder).Sum(l => l.Kost);

    // Tripletex-linjen for en vare: «produktkode navn», veiledende pris som
    // enhetspris og rabatten som gir salgsprisen, og innkjøpsprisen. Uten
    // veiledende pris (eller hvis vi selger over den) går salgsprisen rett inn.
    // visRabatt: bare når tilbudet er priset som «Rabatt på veiledende» skal
    // Tripletex-linjen vise veiledende pris og rabatt %. Med DG, påslag eller
    // veiledende pris sendes bare nettoprisen, uten rabatt.
    // Referansen på Tripletex-ordren: personen som bestilte. Kontakt valgt på
    // arbeidsordren, ellers prosjektets, tilbudets eller kundens kontaktperson.
    // «-» på arbeidsordren betyr ingen kontakt. Uten person: arbeidsordrenummeret.
    public static string TripletexReferanse(Arbeidsordre ordre)
    {
        static string? Tom(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        var valgt = Tom(ordre.Kontaktperson);
        var navn = valgt == "-" ? null
            : valgt ?? Tom(ordre.Prosjekt?.Kontaktperson) ?? Tom(ordre.Tilbud?.KontaktpersonNavn)
                ?? Tom(ordre.Kunde?.Kontaktperson) ?? Tom(ordre.Prosjekt?.Kunde?.Kontaktperson);
        return navn ?? $"Arbeidsordre #{ordre.Id} - {ordre.Tittel}";
    }

    public static bool VisRabattITripletex(Arbeidsordre ordre) =>
        ordre.Tilbud is null || ordre.Tilbud.PrisType == TilbudPrisType.Rabatt;

    public static (string Beskrivelse, decimal Enhetspris, decimal RabattProsent, decimal? Innpris) TripletexLinje(OkonomiLinje l, bool visRabatt = true)
    {
        var navn = l.Navn.Trim();
        var kode = l.Produktkode?.Trim();
        var beskrivelse = string.IsNullOrEmpty(kode) || navn.StartsWith(kode, StringComparison.OrdinalIgnoreCase) ? navn : $"{kode} {navn}";
        var pris = l.Antall == 0 ? 0 : l.Belop / l.Antall;
        if (visRabatt && l.Listepris is { } liste && liste > 0 && pris > 0 && pris < liste)
        {
            var rabatt = Math.Round((1 - pris / liste) * 100m, 4);
            return (beskrivelse, liste, rabatt, l.Innpris);
        }

        return (beskrivelse, Math.Round(pris, 2), 0, l.Innpris);
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
