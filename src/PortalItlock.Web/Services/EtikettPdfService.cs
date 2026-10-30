using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Etiketter for varer i komponentregisteret (som "Lag etikett" i Tripletex):
// velg varer, størrelse og hva som skal vises. Strekkoden lages fra GTIN/EAN
// (EAN-13/EAN-8), ellers Code 128 av GTIN eller produktkoden.
public class EtikettPdfService(ApplicationDbContext db)
{
    // Mål i millimeter. Ark: kolonner x rader med marger og mellomrom som på
    // vanlige A4-etikettark. Rull: én etikett per side (labelskriver).
    public sealed record Format(string Kode, string Navn, float SideB, float SideH, int Kol, int Rad, float B, float H,
        float Venstre, float Topp, float KolGap, float RadGap)
    {
        public bool ErRull => Kol == 1 && Rad == 1;
        public int PerSide => Kol * Rad;
    }

    public static readonly Format[] Formater =
    [
        new("a4-65", "A4-ark, 65 etiketter (38,1 × 21,2 mm)", 210, 297, 5, 13, 38.1f, 21.2f, 4.65f, 10.7f, 2.5f, 0),
        new("a4-40", "A4-ark, 40 etiketter (48,5 × 25,4 mm)", 210, 297, 4, 10, 48.5f, 25.4f, 8f, 21.5f, 0, 0),
        new("a4-24", "A4-ark, 24 etiketter (70 × 37 mm)", 210, 297, 3, 8, 70f, 37f, 0, 0.5f, 0, 0),
        new("a4-21", "A4-ark, 21 etiketter (63,5 × 38,1 mm)", 210, 297, 3, 7, 63.5f, 38.1f, 7.2f, 15.15f, 2.5f, 0),
        new("a4-14", "A4-ark, 14 etiketter (99,1 × 38,1 mm)", 210, 297, 2, 7, 99.1f, 38.1f, 4.65f, 15.15f, 2.5f, 0),
        new("rull-57x32", "Labelskriver, 57 × 32 mm", 57, 32, 1, 1, 57f, 32f, 0, 0, 0, 0),
        new("rull-62x29", "Labelskriver, 62 × 29 mm", 62, 29, 1, 1, 62f, 29f, 0, 0, 0, 0),
        new("rull-102x50", "Labelskriver, 102 × 50 mm", 102, 50, 1, 1, 102f, 50f, 0, 0, 0, 0)
    ];

    public sealed class Valg
    {
        public string Format { get; set; } = "a4-24";
        public bool Navn { get; set; } = true;
        public bool Produktkode { get; set; } = true;
        public bool Leverandor { get; set; }
        public bool PrisInklMva { get; set; } = true;
        public bool PrisEksMva { get; set; }
        public bool Strekkode { get; set; } = true;
        public int AntallPerVare { get; set; } = 1;
        // Eget antall per vare (f.eks. like mange som på lager). Overstyrer AntallPerVare.
        public Dictionary<int, int>? AntallPer { get; set; }
        public int Start { get; set; } = 1;
    }

    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");
    private const decimal Mva = 1.25m;
    private const float MmTilPt = 72f / 25.4f;

    public async Task<byte[]?> GenerateAsync(IReadOnlyList<int> ider, Valg valg)
    {
        var komponenter = await db.Components.Where(c => ider.Contains(c.Id)).ToListAsync();
        if (komponenter.Count == 0)
        {
            return null;
        }

        // Samme rekkefølge som valgt i registeret, og hver vare så mange ganger som ønsket.
        var antall = Math.Clamp(valg.AntallPerVare, 1, 500);
        var etiketter = ider
            .Select(id => komponenter.FirstOrDefault(c => c.Id == id))
            .Where(c => c is not null)
            .SelectMany(c => Enumerable.Repeat(c!, valg.AntallPer is { } per && per.TryGetValue(c!.Id, out var n) ? Math.Clamp(n, 0, 500) : antall))
            .ToList();

        var format = Formater.FirstOrDefault(f => f.Kode == valg.Format) ?? Formater[2];
        var tomme = format.ErRull ? 0 : Math.Clamp(valg.Start, 1, format.PerSide) - 1;
        var plasser = Enumerable.Repeat<Models.Component?>(null, tomme).Concat(etiketter).ToList();

        var document = Document.Create(doc =>
        {
            foreach (var side in plasser.Chunk(format.PerSide))
            {
                doc.Page(page =>
                {
                    page.Size(format.SideB, format.SideH, Unit.Millimetre);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontColor(Colors.Black).LineHeight(1.1f));

                    page.Content()
                        .PaddingLeft(format.Venstre, Unit.Millimetre)
                        .PaddingTop(format.Topp, Unit.Millimetre)
                        .Column(col =>
                        {
                            col.Spacing(format.RadGap, Unit.Millimetre);
                            foreach (var rad in side.Chunk(format.Kol))
                            {
                                col.Item().Height(format.H, Unit.Millimetre).Row(row =>
                                {
                                    row.Spacing(format.KolGap, Unit.Millimetre);
                                    foreach (var c in rad)
                                    {
                                        row.ConstantItem(format.B, Unit.Millimetre).Element(e => Etikett(e, c, format, valg));
                                    }
                                });
                            }
                        });
                });
            }
        });

        return document.GeneratePdf();
    }

    private static void Etikett(IContainer container, Models.Component? c, Format format, Valg valg)
    {
        if (c is null)
        {
            return; // tom plass (startposisjon på et påbegynt ark)
        }

        // Tekst og strekkode skaleres etter etikettens høyde (38 mm ≈ normal).
        var s = Math.Clamp(format.H / 38f, 0.55f, 1.35f);
        var pad = Math.Max(1.4f, format.H * 0.07f);
        var strekkode = valg.Strekkode ? Strekkode.Svg(c.Gtin, c.Produktkode) : null;
        var pris = c.PrisVeiledende;

        container.Padding(pad, Unit.Millimetre).Column(col =>
        {
            col.Spacing(1.2f * s);

            if (valg.Navn)
            {
                col.Item().Text(string.IsNullOrWhiteSpace(c.Navn2) ? c.Navn : $"{c.Navn} {c.Navn2}")
                    .FontSize(9.5f * s).Bold().ClampLines(2);
            }

            var linje = new[] { valg.Produktkode ? c.Produktkode : null, valg.Leverandor ? c.Leverandor ?? c.Produsent : null }
                .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (linje.Count > 0)
            {
                col.Item().Text(string.Join(" · ", linje)).FontSize(7f * s).FontColor(Colors.Grey.Darken2).ClampLines(1);
            }

            if ((valg.PrisInklMva || valg.PrisEksMva) && pris is decimal p)
            {
                col.Item().Text(t =>
                {
                    if (valg.PrisInklMva)
                    {
                        t.Span(FormatKr(p * Mva)).FontSize(13f * s).Bold();
                        t.Span(" inkl. mva").FontSize(6.5f * s).FontColor(Colors.Grey.Darken2);
                    }
                    if (valg.PrisEksMva)
                    {
                        t.Span(valg.PrisInklMva ? "   " : "");
                        t.Span(FormatKr(p)).FontSize((valg.PrisInklMva ? 8f : 13f) * s).Bold();
                        t.Span(" eks. mva").FontSize(6.5f * s).FontColor(Colors.Grey.Darken2);
                    }
                });
            }

            if (strekkode is not null)
            {
                // Strekkoden fyller resten av høyden nederst på etiketten.
                col.Item().ExtendVertical().AlignBottom().AlignLeft().MaxHeight(format.H * 0.45f, Unit.Millimetre)
                    .Svg(strekkode).FitArea();
            }
        });
    }

    private static string FormatKr(decimal v) => v.ToString(v % 1 == 0 ? "N0" : "N2", Kultur) + " kr";
}
