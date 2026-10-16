using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class TimeoversiktService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public async Task<List<Timeregistrering>> HentRegistreringerAsync(DateTime fra, DateTime til, int? montorId, bool kunGodkjent = false)
    {
        var query = db.Timeregistreringer
            .Include(t => t.Montor)
            .Include(t => t.Arbeidsordre)
            .Where(t => t.Dato >= fra.Date && t.Dato <= til.Date)
            .AsQueryable();

        if (montorId.HasValue)
        {
            query = query.Where(t => t.MontorId == montorId.Value);
        }

        if (kunGodkjent)
        {
            query = query.Where(t => t.Status == TimeregistreringStatus.Godkjent);
        }

        return await query
            .OrderBy(t => t.Montor!.Navn).ThenBy(t => t.Dato)
            .ToListAsync();
    }

    public byte[] GenererCsv(List<Timeregistrering> registreringer)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', "Montør", "Dato", "Type", "Timer", "Arbeidsordre", "Kommentar"));

        foreach (var t in registreringer)
        {
            var timer = t.TotalTimer.ToString("F2", Kultur);
            sb.AppendLine(string.Join(';',
                CsvFelt(t.Montor?.Navn),
                CsvFelt(t.Dato.ToString("dd.MM.yyyy")),
                CsvFelt(t.Type.Visningsnavn()),
                CsvFelt(timer),
                CsvFelt(t.Arbeidsordre?.Tittel),
                CsvFelt(t.Kommentar)));
        }

        return new UTF8Encoding(true).GetBytes(sb.ToString());
    }

    private static string CsvFelt(string? verdi) =>
        $"\"{(verdi ?? "").Replace("\"", "\"\"")}\"";

    // Godkjent fravær som overlapper perioden - kommer med i lønnsgrunnlaget.
    public async Task<List<FravarSoknad>> HentFravarAsync(DateTime fra, DateTime til, int? brukerId)
    {
        var query = db.FravarSoknader
            .Include(f => f.Bruker)
            .Where(f => f.Status == FravarStatus.Godkjent && f.FraDato <= til.Date && f.TilDato >= fra.Date);

        if (brukerId.HasValue)
        {
            query = query.Where(f => f.BrukerId == brukerId.Value);
        }

        return await query.ToListAsync();
    }

    // Timer i perioden som fortsatt venter på godkjenning (ikke med i PDF-en).
    public async Task<decimal> HentVenterTimerAsync(DateTime fra, DateTime til, int? montorId)
    {
        var query = db.Timeregistreringer
            .Where(t => t.Status == TimeregistreringStatus.Venter && t.Dato >= fra.Date && t.Dato <= til.Date);

        if (montorId.HasValue)
        {
            query = query.Where(t => t.MontorId == montorId.Value);
        }

        var venter = await query.ToListAsync();
        return venter.Sum(t => t.TotalTimer);
    }

    // Lønnsgrunnlag (L1-D): side 1 har én liten boks per ansatt (som en mini
    // lønnsslipp) med arbeidstid, overtid, avspasering, fravær og kjøring, pluss
    // en totalboks. Bakerst kommer registreringene per ansatt til kontroll.
    // Navn, logo og farge hentes fra organisasjonen som er logget inn.
    public byte[] GenererPdf(List<Timeregistrering> registreringer, List<FravarSoknad> fravar, decimal venterTimer,
        DateTime fra, DateTime til, string? montorNavn)
    {
        var org = RapportProfil.Fra(tenantContext.Current);
        var ansatte = LagAnsattSummer(registreringer, fravar, fra, til);
        var totalTimer = ansatte.Sum(a => a.Sum);
        var periode = PeriodeTekst(fra, til);
        var helManed = fra.Day == 1 && fra.Month == til.Month && fra.Year == til.Year
            && til.Day == DateTime.DaysInMonth(til.Year, til.Month);
        var tittelPeriode = helManed ? Stor(fra.ToString("MMMM yyyy", Kultur)) : periode;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                RapportMal.Side(page);

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    // Topp
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(v =>
                        {
                            v.Item().Element(e => org.TegnLogo(e, pdfLogo, 26));
                            v.Item().PaddingTop(12).Text("Lønnsgrunnlag").FontSize(22).Bold();
                            v.Item().Text(string.Join(" · ", new[]
                            {
                                tittelPeriode,
                                montorNavn ?? $"{ansatte.Count} {(ansatte.Count == 1 ? "ansatt" : "ansatte")}",
                                FormatTimer(totalTimer)
                            })).FontSize(10).FontColor(RapportMal.Dempet);
                        });
                        row.ConstantItem(150).AlignRight().Column(h =>
                        {
                            h.Item().AlignRight().Text("Periode").FontSize(8).FontColor(RapportMal.Dempet);
                            h.Item().AlignRight().Text(periode).FontSize(12).Bold();
                            h.Item().AlignRight().Text($"Laget {DateTime.Today:dd.MM.yyyy}").FontSize(8).FontColor(RapportMal.Svak);
                        });
                    });
                    col.Item().LineHorizontal(2).LineColor(org.Aksent);

                    if (ansatte.Count == 0)
                    {
                        col.Item().PaddingTop(10).Text("Ingen godkjente timer eller fravær i perioden.").FontColor(RapportMal.Dempet);
                    }
                    else
                    {
                        // To kolonner med én boks per ansatt, totalen som siste boks.
                        var bokser = ansatte.Select(a => (a.Navn, a.Sum, Linjer: a.Linjer(), ErTotal: false)).ToList();
                        bokser.Add(("Totalt", totalTimer, TotalLinjer(ansatte), true));

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            foreach (var b in bokser)
                            {
                                table.Cell().Padding(5).Element(e => TegnBoks(e, b.Navn, b.Sum, b.Linjer, b.ErTotal, org));
                            }
                        });
                    }

                    if (venterTimer > 0)
                    {
                        col.Item().Background(Color.FromHex("#FDF6EA")).Padding(8).Text(t =>
                        {
                            t.Span($"{FormatTimer(venterTimer)} venter på godkjenning").Bold().FontColor(Color.FromHex("#5A4212"));
                            t.Span(" og er ikke med. Godkjenn dem i Timegodkjenning og lag PDF-en på nytt.").FontColor(Color.FromHex("#6F531A"));
                        });
                    }
                    else
                    {
                        col.Item().Background(Color.FromHex("#EEF6F0")).Padding(8)
                            .Text("✓ Alle timer i perioden er godkjent").Bold().FontColor(Color.FromHex("#2E7D4F"));
                    }

                    // Signatur
                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Element(e => Signatur(e, "Godkjent av"));
                        row.ConstantItem(34);
                        row.RelativeItem().Element(e => Signatur(e, "Mottatt av lønn"));
                    });

                    // Registreringene per ansatt, til kontroll
                    if (ansatte.Count > 0)
                    {
                        col.Item().PageBreak();
                        col.Item().Text("Registreringer").FontSize(15).Bold();

                        foreach (var a in ansatte)
                        {
                            col.Item().PaddingTop(4).Column(blokk =>
                            {
                                blokk.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(a.Navn).FontSize(11).Bold();
                                    r.AutoItem().Text($"{FormatTimer(a.Sum)}{(a.Km > 0 ? $" · {a.Km.ToString("N0", Kultur)} km" : "")}")
                                        .FontSize(9).Bold().FontColor(RapportMal.Dempet);
                                });
                                blokk.Item().PaddingTop(2).LineHorizontal(1).LineColor(PdfStil.Ink);
                                blokk.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(62);
                                        c.ConstantColumn(118);
                                        c.RelativeColumn();
                                        c.ConstantColumn(44);
                                        c.ConstantColumn(40);
                                    });

                                    foreach (var rad in a.Rader)
                                    {
                                        table.Cell().Element(Celle).Text(rad.Dato);
                                        table.Cell().Element(Celle).Text(t =>
                                        {
                                            t.Span(rad.Tid).FontColor(rad.ErFravar ? FravarFarge(rad.Tid) : PdfStil.Ink);
                                            if (rad.Type is not null)
                                            {
                                                t.Span($"  {rad.Type}").Bold();
                                            }
                                        });
                                        table.Cell().Element(Celle).Text(rad.Beskrivelse);
                                        table.Cell().Element(Celle).AlignRight().Text(rad.Km);
                                        table.Cell().Element(Celle).AlignRight().Text(rad.Timer);
                                    }
                                });
                            });
                        }
                    }
                });

                page.Footer().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text($"{org.Navn} · Lønnsgrunnlag {(helManed ? fra.ToString("MMMM yyyy", Kultur) : periode)}").FontSize(7).FontColor(RapportMal.Svak);
                    row.AutoItem().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(7).FontColor(RapportMal.Svak));
                        t.Span("Side ");
                        t.CurrentPageNumber();
                        t.Span(" av ");
                        t.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer Celle(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(RapportMal.Linje).PaddingVertical(3).PaddingHorizontal(3);

    private static void TegnBoks(IContainer container, string navn, decimal sum, List<(string Etikett, string Verdi, Color? Farge)> linjer,
        bool erTotal, RapportProfil org)
    {
        var boks = erTotal
            ? container.Background(org.AksentLys).Padding(10)
            : container.Border(0.7f).BorderColor(RapportMal.Linje).Padding(10);

        boks.Column(c =>
        {
            c.Item().Row(r =>
            {
                r.RelativeItem().Text(navn).FontSize(11).Bold();
                r.AutoItem().Text(FormatTimer(sum)).FontSize(11).Bold();
            });
            c.Item().PaddingTop(4);
            foreach (var (etikett, verdi, farge) in linjer)
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(etikett).FontSize(8.5f).FontColor(farge ?? RapportMal.Dempet);
                    r.AutoItem().Text(verdi).FontSize(8.5f).FontColor(farge ?? PdfStil.Ink);
                });
            }
        });
    }

    private static void Signatur(IContainer container, string tittel) =>
        container.BorderTop(0.7f).BorderColor(PdfStil.Ink).PaddingTop(4).Column(c =>
        {
            c.Item().Text(tittel).FontSize(8.5f).Bold();
            c.Item().Text("Navn, dato og signatur").FontSize(7.5f).FontColor(RapportMal.Dempet);
        });

    private static readonly Color Blaa = Color.FromHex("#2457C5");
    private static readonly Color Rod = Color.FromHex("#B4462C");

    private static Color FravarFarge(string type) => type.StartsWith("Ferie") ? Blaa : Rod;

    private static List<AnsattSum> LagAnsattSummer(List<Timeregistrering> registreringer, List<FravarSoknad> fravar, DateTime fra, DateTime til)
    {
        var personer = registreringer.Select(t => (Id: t.MontorId, Navn: t.Montor?.Navn ?? "-"))
            .Concat(fravar.Select(f => (Id: f.BrukerId, Navn: f.Bruker?.Navn ?? "-")))
            .DistinctBy(x => x.Id);

        return personer.Select(p =>
            {
                var regs = registreringer.Where(t => t.MontorId == p.Id).OrderBy(t => t.Dato).ThenBy(t => t.Start).ToList();
                var fr = fravar.Where(f => f.BrukerId == p.Id).OrderBy(f => f.FraDato).ToList();

                var a = new AnsattSum
                {
                    Navn = p.Navn,
                    Normal = regs.Where(t => t.Type == TimeregistreringType.NormalArbeidstid).Sum(t => t.TotalTimer),
                    Overtid50 = regs.Where(t => t.Type == TimeregistreringType.Overtid50).Sum(t => t.TotalTimer),
                    Overtid100 = regs.Where(t => t.Type == TimeregistreringType.Overtid100).Sum(t => t.TotalTimer),
                    Avspasering = regs.Where(t => t.Type == TimeregistreringType.Avspasering).Sum(t => t.TotalTimer),
                    Km = regs.Sum(t => t.Kilometer),
                };

                foreach (var f in fr)
                {
                    var (dager, timer) = FravarIPerioden(f, fra, til);
                    a.Fravar.Add((f.Type.Visningsnavn(), dager, timer));
                }

                a.Rader = regs.Select(t => new DetaljRad(
                        t.Dato,
                        t.Dato.ToString("dd.MM", Kultur),
                        $"{t.Start:hh\\:mm}–{t.Slutt:hh\\:mm}",
                        t.Type switch
                        {
                            TimeregistreringType.Overtid50 => "OT 50 %",
                            TimeregistreringType.Overtid100 => "OT 100 %",
                            TimeregistreringType.Avspasering => "Avspas.",
                            _ => null
                        },
                        t.Arbeidsordre?.Tittel ?? "",
                        t.Kilometer > 0 ? t.Kilometer.ToString("N0", Kultur) : "",
                        t.TotalTimer.ToString("N1", Kultur),
                        false))
                    .Concat(fr.Select(f => new DetaljRad(
                        f.FraDato,
                        f.FraDato.Date == f.TilDato.Date ? f.FraDato.ToString("dd.MM", Kultur) : $"{f.FraDato:dd.MM}–{f.TilDato:dd.MM}",
                        f.Type.Visningsnavn(),
                        null,
                        f.HeleDagen ? "" : $"{f.StartTid:HH\\:mm}–{f.SluttTid:HH\\:mm}",
                        "",
                        "–",
                        true)))
                    .OrderBy(r => r.Sortering)
                    .ToList();

                return a;
            })
            .OrderBy(a => a.Navn, StringComparer.Create(Kultur, true))
            .ToList();
    }

    // Arbeidsdager (man–fre) av søknaden som ligger innenfor perioden. Søknader
    // for deler av en dag telles i timer i stedet.
    private static (int Dager, decimal Timer) FravarIPerioden(FravarSoknad f, DateTime fra, DateTime til)
    {
        if (!f.HeleDagen && f.StartTid is { } s && f.SluttTid is { } e)
        {
            return (0, (decimal)Math.Max(0, (e - s).TotalHours));
        }

        var start = f.FraDato.Date < fra.Date ? fra.Date : f.FraDato.Date;
        var slutt = f.TilDato.Date > til.Date ? til.Date : f.TilDato.Date;

        var dager = 0;
        for (var d = start; d <= slutt; d = d.AddDays(1))
        {
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                dager++;
            }
        }

        return (dager, 0);
    }

    private static List<(string, string, Color?)> TotalLinjer(List<AnsattSum> ansatte)
    {
        var linjer = new List<(string, string, Color?)>
        {
            ("Arbeidstid", FormatTimer(ansatte.Sum(a => a.Normal)), null)
        };

        var ot = ansatte.Sum(a => a.Overtid50 + a.Overtid100);
        if (ot > 0)
        {
            linjer.Add(("Overtid", FormatTimer(ot), null));
        }

        var avsp = ansatte.Sum(a => a.Avspasering);
        if (avsp > 0)
        {
            linjer.Add(("Avspasering", FormatTimer(avsp), null));
        }

        var dager = ansatte.Sum(a => a.Fravar.Sum(f => f.Dager));
        var timer = ansatte.Sum(a => a.Fravar.Sum(f => f.Timer));
        if (dager > 0 || timer > 0)
        {
            linjer.Add(("Fravær", FravarTekst(dager, timer), null));
        }

        var km = ansatte.Sum(a => a.Km);
        if (km > 0)
        {
            linjer.Add(("Kjøring", $"{km.ToString("N0", Kultur)} km", null));
        }

        return linjer;
    }

    private static string FravarTekst(int dager, decimal timer) => string.Join(" + ", new[]
    {
        dager > 0 ? $"{dager} {(dager == 1 ? "dag" : "dager")}" : null,
        timer > 0 ? FormatTimer(timer) : null
    }.Where(s => s is not null));

    private static string PeriodeTekst(DateTime fra, DateTime til) =>
        fra.Year == til.Year
            ? $"{fra:dd.MM} – {til:dd.MM.yyyy}"
            : $"{fra:dd.MM.yyyy} – {til:dd.MM.yyyy}";

    private static string Stor(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Kultur) + s[1..];

    private static string FormatTimer(decimal value) => value.ToString("N1", Kultur) + " t";

    private sealed record DetaljRad(DateTime Sortering, string Dato, string Tid, string? Type, string Beskrivelse, string Km, string Timer, bool ErFravar);

    private sealed class AnsattSum
    {
        public string Navn { get; set; } = "";
        public decimal Normal { get; set; }
        public decimal Overtid50 { get; set; }
        public decimal Overtid100 { get; set; }
        public decimal Avspasering { get; set; }
        public decimal Km { get; set; }
        public List<(string Type, int Dager, decimal Timer)> Fravar { get; } = [];
        public List<DetaljRad> Rader { get; set; } = [];
        public decimal Sum => Normal + Overtid50 + Overtid100 + Avspasering;

        // Bare det som faktisk finnes - ingen tomme linjer.
        public List<(string, string, Color?)> Linjer()
        {
            var linjer = new List<(string, string, Color?)>();
            if (Normal > 0) linjer.Add(("Arbeidstid", FormatTimer(Normal), null));
            if (Overtid50 > 0) linjer.Add(("Overtid 50 %", FormatTimer(Overtid50), null));
            if (Overtid100 > 0) linjer.Add(("Overtid 100 %", FormatTimer(Overtid100), null));
            if (Avspasering > 0) linjer.Add(("Avspasering", FormatTimer(Avspasering), null));
            foreach (var g in Fravar.GroupBy(f => f.Type))
            {
                var tekst = FravarTekst(g.Sum(f => f.Dager), g.Sum(f => f.Timer));
                if (tekst.Length > 0)
                {
                    linjer.Add((g.Key, tekst, FravarFarge(g.Key)));
                }
            }
            if (Km > 0) linjer.Add(("Kjøring", $"{Km.ToString("N0", Kultur)} km", null));
            return linjer;
        }
    }
}
