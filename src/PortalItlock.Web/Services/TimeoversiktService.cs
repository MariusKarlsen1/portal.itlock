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
    // Lønnsgrunnlag (forslag N1): ren hvit topp, tallene i fire tynne bokser og én
    // tabell med tynne linjer. Registreringene følger på egne sider, til kontroll.
    public byte[] GenererPdf(List<Timeregistrering> registreringer, List<FravarSoknad> fravar, decimal venterTimer,
        DateTime fra, DateTime til, string? montorNavn)
    {
        var org = RapportProfil.Fra(tenantContext.Current);
        var ansatte = LagAnsattSummer(registreringer, fravar, fra, til);
        var totalTimer = ansatte.Sum(a => a.Sum);
        var periode = PeriodeTekst(fra, til);
        var helManed = fra.Day == 1 && fra.Month == til.Month && fra.Year == til.Year
            && til.Day == DateTime.DaysInMonth(til.Year, til.Month);
        var maned = fra.ToString("MMMM yyyy", Kultur);
        var periodeTittel = helManed ? $"{Stor(maned)} · {fra.Day}.–{til.Day}. {fra.ToString("MMMM", Kultur)}" : periode;
        var undertittel = montorNavn is null ? periodeTittel : $"{periodeTittel} · {montorNavn}";
        var bunntekst = $"{org.Navn} · Lønnsgrunnlag {(helManed ? maned : periode)} · laget {DateTime.Today:dd.MM.yyyy}";

        var fravarDager = ansatte.Sum(a => a.Fravar.Sum(f => f.Dager));
        var fravarTimer = ansatte.Sum(a => a.Fravar.Sum(f => f.Timer));
        var fravarTotal = FravarTekst(fravarDager, fravarTimer);
        var km = ansatte.Sum(a => a.Km);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                RapportMal.Side(page);
                page.Content().Column(col =>
                {
                    col.Spacing(11);
                    col.Item().Element(e => Tittel(e, org, "Lønnsgrunnlag", undertittel));

                    col.Item().Row(row =>
                    {
                        row.Spacing(6);
                        Tall(row, "Godkjente timer", FormatKort(totalTimer), org.Aksent);
                        Tall(row, "Ansatte", ansatte.Count.ToString(Kultur), null);
                        Tall(row, "Fravær", fravarTotal.Length > 0 ? fravarTotal : "Ingen", null);
                        Tall(row, "Kjørt", KmTekst(km), null);
                    });

                    if (ansatte.Count == 0)
                    {
                        col.Item().PaddingVertical(10).Text("Ingen godkjente timer eller fravær i perioden.").FontColor(Mut);
                    }
                    else
                    {
                        col.Item().Element(e => AnsattTabell(e, ansatte, org));
                        col.Item().Column(sum =>
                        {
                            sum.Item().LineHorizontal(1.5f).LineColor(PdfStil.Ink);
                            sum.Item().PaddingTop(6).PaddingHorizontal(3).Row(r =>
                            {
                                var deler = new List<string> { "Totalt" };
                                if (fravarTotal.Length > 0) deler.Add($"{fravarTotal} fravær");
                                if (km > 0) deler.Add(KmTekst(km));
                                r.RelativeItem().AlignBottom().Text(string.Join(" · ", deler)).FontColor(Mut);
                                r.AutoItem().Text(FormatKort(totalTimer)).FontSize(12).Bold();
                            });
                        });
                    }

                    col.Item().Element(e => StatusBoks(e, venterTimer));

                    // Signaturene står nederst på siden.
                    col.Item().Extend().AlignBottom().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Element(e => Signatur(e, "Godkjent av"));
                        row.ConstantItem(26);
                        row.RelativeItem().Element(e => Signatur(e, "Mottatt av lønn"));
                    });
                });
                page.Footer().Element(e => Bunn(e, bunntekst));
            });

            // Registreringene per ansatt, til kontroll.
            if (ansatte.Count > 0)
            {
                container.Page(page =>
                {
                    RapportMal.Side(page);
                    page.Header().PaddingBottom(10).Element(e => Tittel(e, org, "Registreringer", $"{(helManed ? Stor(maned) : periode)} · godkjente timer"));
                    page.Content().Column(col =>
                    {
                        col.Spacing(13);
                        foreach (var a in ansatte)
                        {
                            col.Item().EnsureSpace(70).Column(blokk =>
                            {
                                blokk.Item().PaddingBottom(5).Row(r =>
                                {
                                    r.RelativeItem().Element(e => Navn(e, a.Navn, org, 10.5f));
                                    r.Spacing(4);
                                    if (a.Sum > 0) r.AutoItem().AlignMiddle().Element(e => Merke(e, FormatKort(a.Sum)));
                                    if (a.Km > 0) r.AutoItem().AlignMiddle().Element(e => Merke(e, KmTekst(a.Km)));
                                });
                                blokk.Item().Element(e => RegistreringTabell(e, a));
                            });
                        }
                    });
                    page.Footer().Element(e => Bunn(e, bunntekst));
                });
            }
        });

        return document.GeneratePdf();
    }

    private static readonly Color Mut = Color.FromHex("#66708A");
    private static readonly Color Strek = Color.FromHex("#E6EAF1");
    private static readonly Color Tom = Color.FromHex("#B1B8C7");

    private void Tittel(IContainer container, RapportProfil org, string tittel, string undertittel) =>
        container.Row(row =>
        {
            row.RelativeItem().Column(v =>
            {
                v.Item().Text(tittel).FontSize(21).Bold();
                v.Item().Text(undertittel).FontSize(10).FontColor(Mut);
            });
            row.ConstantItem(150).AlignRight().Element(e => org.TegnLogo(e, pdfLogo, 26));
        });

    private static void Tall(RowDescriptor row, string etikett, string verdi, Color? farge) =>
        row.RelativeItem().Border(0.75f).BorderColor(Strek).CornerRadius(8).PaddingVertical(7).PaddingHorizontal(9).Column(c =>
        {
            c.Item().Text(etikett).FontSize(7.5f).FontColor(Mut);
            c.Item().Text(verdi).FontSize(13).Bold().FontColor(farge ?? PdfStil.Ink);
        });

    private static void AnsattTabell(IContainer container, List<AnsattSum> ansatte, RapportProfil org)
    {
        // Avspasering får bare kolonne når noen faktisk har det.
        var harAvspasering = ansatte.Any(a => a.Avspasering > 0);

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.3f);
                c.ConstantColumn(40);
                c.ConstantColumn(40);
                c.ConstantColumn(44);
                if (harAvspasering)
                {
                    c.ConstantColumn(44);
                }

                c.RelativeColumn(1.8f);
                c.ConstantColumn(46);
                c.ConstantColumn(46);
            });

            table.Header(h =>
            {
                Hode(h.Cell(), "Ansatt");
                Hode(h.Cell().AlignRight(), "Normal");
                Hode(h.Cell().AlignRight(), "OT 50 %");
                Hode(h.Cell().AlignRight(), "OT 100 %");
                if (harAvspasering)
                {
                    Hode(h.Cell().AlignRight(), "Avspas.");
                }

                Hode(h.Cell().PaddingLeft(6), "Fravær");
                Hode(h.Cell().AlignRight(), "Kjørt");
                Hode(h.Cell().AlignRight(), "Sum");
            });

            foreach (var a in ansatte)
            {
                table.Cell().Element(Rad).Element(e => Navn(e, a.Navn, org, 9.5f));
                TimerCelle(table.Cell(), a.Normal);
                TimerCelle(table.Cell(), a.Overtid50);
                TimerCelle(table.Cell(), a.Overtid100);
                if (harAvspasering)
                {
                    TimerCelle(table.Cell(), a.Avspasering);
                }

                var merker = a.Fravar.GroupBy(f => f.Type)
                    .Select(g => $"{g.Key} {KortFravar(g.Sum(f => f.Dager), g.Sum(f => f.Timer))}")
                    .ToList();
                table.Cell().Element(Rad).PaddingLeft(6).Element(e =>
                {
                    if (merker.Count == 0)
                    {
                        e.Text("–").FontColor(Tom);
                        return;
                    }

                    e.Inlined(i =>
                    {
                        i.Spacing(3);
                        foreach (var m in merker)
                        {
                            i.Item().Element(x => Merke(x, m));
                        }
                    });
                });

                table.Cell().Element(Rad).AlignRight().Text(a.Km > 0 ? KmTekst(a.Km) : "–").FontColor(a.Km > 0 ? PdfStil.Ink : Tom);
                table.Cell().Element(Rad).AlignRight().Text(FormatKort(a.Sum)).Bold();
            }
        });
    }

    private static void RegistreringTabell(IContainer container, AnsattSum a) =>
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(66);
                c.ConstantColumn(104);
                c.RelativeColumn();
                c.ConstantColumn(36);
                c.ConstantColumn(38);
            });

            table.Header(h =>
            {
                Hode(h.Cell(), "Dato");
                Hode(h.Cell(), "Tid");
                Hode(h.Cell(), "Arbeid");
                Hode(h.Cell().AlignRight(), "Km");
                Hode(h.Cell().AlignRight(), "Timer");
            });

            foreach (var rad in a.Rader)
            {
                table.Cell().Element(Rad).Text(rad.Dato).Bold();
                table.Cell().Element(Rad).Text(t =>
                {
                    t.Span(rad.Tid).FontColor(rad.ErFravar ? FravarFarge(rad.Tid) : Mut);
                    if (rad.Type is not null)
                    {
                        t.Span($"  {rad.Type}").Bold();
                    }
                });
                table.Cell().Element(Rad).Text(rad.Beskrivelse);
                table.Cell().Element(Rad).AlignRight().Text(rad.Km);
                table.Cell().Element(Rad).AlignRight().Text(rad.Timer).Bold();
            }
        });

    private static void Hode(IContainer cell, string tekst) =>
        cell.PaddingHorizontal(3).PaddingBottom(4).Text(tekst.ToUpper(Kultur)).FontSize(6.5f).Bold().FontColor(Mut).LetterSpacing(0.05f);

    private static IContainer Rad(IContainer c) =>
        c.BorderTop(0.6f).BorderColor(Strek).PaddingVertical(4.5f).PaddingHorizontal(3).AlignMiddle();

    private static void TimerCelle(IContainer cell, decimal timer) =>
        cell.Element(Rad).AlignRight().Text(timer > 0 ? timer.ToString("N1", Kultur) : "–").FontColor(timer > 0 ? PdfStil.Ink : Tom);

    private static void Navn(IContainer container, string navn, RapportProfil org, float storrelse) =>
        container.Row(r =>
        {
            r.Spacing(6);
            r.AutoItem().AlignMiddle().Width(17).Height(17).Background(org.AksentLys).CornerRadius(8.5f)
                .AlignCenter().AlignMiddle().Text(Initialer(navn)).FontSize(6.5f).Bold().FontColor(org.Aksent);
            r.RelativeItem().AlignMiddle().Text(navn).FontSize(storrelse).Bold();
        });

    private static void Merke(IContainer container, string tekst) =>
        container.Border(0.6f).BorderColor(Strek).CornerRadius(7).PaddingVertical(1).PaddingHorizontal(6)
            .Text(tekst).FontSize(7.5f).SemiBold();

    private static void StatusBoks(IContainer container, decimal venterTimer)
    {
        var venter = venterTimer > 0;
        container
            .Background(Color.FromHex(venter ? "#FEFAF0" : "#F2FAF5"))
            .Border(0.75f).BorderColor(Color.FromHex(venter ? "#EBD9A8" : "#BFE0CC"))
            .CornerRadius(8).PaddingVertical(7).PaddingHorizontal(10)
            .Text(t =>
            {
                var farge = Color.FromHex(venter ? "#6A4F10" : "#1F7A45");
                t.DefaultTextStyle(x => x.FontColor(farge));
                if (venter)
                {
                    t.Span($"{FormatKort(venterTimer)} venter på godkjenning").Bold();
                    t.Span(" og er ikke med. Godkjenn dem og lag PDF-en på nytt.");
                }
                else
                {
                    t.Span("✓ Alle timer i perioden er godkjent").Bold();
                }
            });
    }

    private static void Signatur(IContainer container, string tittel) =>
        container.BorderTop(0.75f).BorderColor(PdfStil.Ink).PaddingTop(4).Column(c =>
        {
            c.Item().Text(tittel).FontSize(8.5f).Bold();
            c.Item().Text("Navn, dato og signatur").FontSize(7.5f).FontColor(Mut);
        });

    private static void Bunn(IContainer container, string tekst) =>
        container.PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(tekst).FontSize(7).FontColor(RapportMal.Svak);
            row.AutoItem().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(7).FontColor(RapportMal.Svak));
                t.Span("Side ");
                t.CurrentPageNumber();
                t.Span(" av ");
                t.TotalPages();
            });
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

    private static string FravarTekst(int dager, decimal timer) => string.Join(" + ", new[]
    {
        dager > 0 ? $"{dager} {(dager == 1 ? "dag" : "dager")}" : null,
        timer > 0 ? FormatKort(timer) : null
    }.Where(s => s is not null));

    private static string PeriodeTekst(DateTime fra, DateTime til) =>
        fra.Year == til.Year
            ? $"{fra:dd.MM} – {til:dd.MM.yyyy}"
            : $"{fra:dd.MM.yyyy} – {til:dd.MM.yyyy}";

    private static string Stor(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Kultur) + s[1..];

    private static string FormatKort(decimal value) => value.ToString("N1", Kultur) + " t";

    private static string KmTekst(decimal km) => $"{km.ToString("N0", Kultur)} km";

    private static string KortFravar(int dager, decimal timer) => string.Join(" + ", new[]
    {
        dager > 0 ? $"{dager} d" : null,
        timer > 0 ? FormatKort(timer) : null
    }.Where(s => s is not null));

    private static string Initialer(string navn)
    {
        var deler = navn.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return deler.Length switch
        {
            0 => "?",
            1 => deler[0][..1].ToUpper(Kultur),
            _ => (deler[0][..1] + deler[^1][..1]).ToUpper(Kultur)
        };
    }

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
    }
}
