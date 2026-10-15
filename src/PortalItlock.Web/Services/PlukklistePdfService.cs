using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class PlukklistePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<List<LagretPdfLinje>> GetSnapshotLinjerAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component)
            .ToListAsync();

        return behov
            .Where(k => k.Component is not null)
            .GroupBy(k => k.Component!.Navn)
            .Select(g => new LagretPdfLinje(g.Key, g.Sum(x => x.Antall), null))
            .OrderBy(l => l.Navn)
            .ToList();
    }

    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component).ThenInclude(c => c!.Type)
            .ToListAsync();

        var gruppert = behov
            .Where(k => k.Component is not null)
            .GroupBy(k => k.ComponentId)
            .Select(g => new { ComponentId = g.Key, Component = g.First().Component!, Antall = g.Sum(x => x.Antall) })
            .OrderBy(x => x.Component.Type?.Navn)
            .ThenBy(x => x.Component.Navn)
            .ToList();

        if (gruppert.Count == 0)
        {
            return null;
        }

        var linjer = await db.PlukklisteLinjer
            .Where(p => p.ProsjektId == prosjektId)
            .ToListAsync();

        // Enkel utskriftsvennlig mal: organisasjonens logo og farge, prosjektinfo,
        // tabell med avkrysningsrute, sumrad, "Må bestilles" per leverandør og
        // signaturlinjer. Liggende A4 slik at alle kolonnene får plass.
        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = prosjekt.KundeId is { } kid ? await db.Kunder.FindAsync(kid) : null;
        var byggetrinnTekst = valgteByggetrinn is not null ? string.Join(", ", valgteByggetrinn) : "Alle";
        var lys = Color.FromHex("#ECE4D6");

        var rader = gruppert.Select(g =>
        {
            var linje = linjer.FirstOrDefault(l => l.ComponentId == g.ComponentId);
            var bestilt = linje?.VarerBestilt ?? 0;
            return new
            {
                g.Component,
                g.Antall,
                Plukket = linje?.AntallPlukket ?? 0,
                Bestilt = bestilt,
                Forventet = linje?.ForventetLevert,
                MaBestilles = Math.Max(g.Antall - bestilt, 0)
            };
        }).ToList();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(PdfStil.Ink));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignBottom().Column(c =>
                        {
                            c.Item().Text("Plukkliste").FontSize(18).Bold();
                            c.Item().PaddingTop(2).Text(prosjekt.Navn).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(160).AlignBottom().AlignRight().Element(e => org.TegnLogo(e, pdfLogo, 24));
                    });
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(org.Aksent);
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        void Felt(string etikett, string verdi) => row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(etikett.ToUpperInvariant()).FontSize(6.5f).Bold().FontColor(org.Aksent).LetterSpacing(0.06f);
                            c.Item().Text(verdi).FontSize(9).SemiBold();
                        });
                        Felt("Kunde", kunde?.Navn ?? "-");
                        Felt("Byggetrinn", byggetrinnTekst);
                        Felt("Utskrevet", DateTime.Now.ToString("dd.MM.yyyy"));
                        Felt("Varer / stk", $"{rader.Count} varer · {rader.Sum(r => r.Antall)} stk");
                        Felt("Må bestilles", $"{rader.Sum(r => r.MaBestilles)} stk");
                    });
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(3f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.1f);
                            c.ConstantColumn(34);
                        });

                        table.Header(h =>
                        {
                            IContainer Hode() => h.Cell().Background(org.Aksent).PaddingVertical(5).PaddingHorizontal(5);
                            Hode().Text("#").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().Text("Beslagstype").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().Text("Varenr").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().Text("Varenavn").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().Text("Leverandør").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().Text("Overflate").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().AlignRight().Text("Antall").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().AlignRight().Text("Plukket").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().AlignRight().Text("Bestilt").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().AlignRight().Text("Må best.").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().Text("Forventet").FontSize(7.5f).Bold().FontColor(Colors.White);
                            Hode().AlignCenter().Text("✓").FontSize(7.5f).Bold().FontColor(Colors.White);
                        });

                        var i = 0;
                        foreach (var r in rader)
                        {
                            i++;
                            IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(lys).PaddingVertical(4).PaddingHorizontal(5);
                            Rad().Text(i.ToString()).FontColor(Colors.Grey.Darken1);
                            Rad().Text(r.Component.Type?.Navn ?? "");
                            Rad().Text(r.Component.Produktkode ?? "").FontColor(Colors.Grey.Darken2);
                            Rad().Text(r.Component.Navn).SemiBold();
                            Rad().Text(r.Component.Leverandor ?? "").FontColor(Colors.Grey.Darken2);
                            Rad().Text(r.Component.Overflate ?? "").FontColor(Colors.Grey.Darken2);
                            Rad().AlignRight().Text($"{r.Antall} {r.Component.Enhet ?? "stk"}").Bold();
                            Rad().AlignRight().Text(r.Plukket.ToString());
                            Rad().AlignRight().Text(r.Bestilt.ToString());
                            var ma = Rad().AlignRight().Text(r.MaBestilles.ToString());
                            if (r.MaBestilles > 0)
                            {
                                ma.Bold().FontColor(Color.FromHex("#C2412B"));
                            }
                            Rad().Text(r.Forventet?.ToString("dd.MM.yy") ?? "");
                            Rad().AlignCenter().AlignMiddle().Width(10).Height(10).Border(0.8f).BorderColor(Colors.Grey.Darken1);
                        }

                        IContainer Sum() => table.Cell().Background(Color.FromHex("#FBF8F3")).PaddingVertical(5).PaddingHorizontal(5);
                        Sum();
                        table.Cell().ColumnSpan(5).Background(Color.FromHex("#FBF8F3")).PaddingVertical(5).PaddingHorizontal(5).Text("Totalt").Bold();
                        Sum().AlignRight().Text(rader.Sum(r => r.Antall).ToString()).Bold();
                        Sum().AlignRight().Text(rader.Sum(r => r.Plukket).ToString()).Bold();
                        Sum().AlignRight().Text(rader.Sum(r => r.Bestilt).ToString()).Bold();
                        Sum().AlignRight().Text(rader.Sum(r => r.MaBestilles).ToString()).Bold().FontColor(Color.FromHex("#C2412B"));
                        Sum();
                        Sum();
                    });

                    var maBestilles = rader.Where(r => r.MaBestilles > 0).ToList();
                    if (maBestilles.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Må bestilles").FontSize(11).Bold().FontColor(org.Aksent);
                        col.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.6f);
                                c.RelativeColumn(1.4f);
                                c.RelativeColumn(4f);
                                c.RelativeColumn(1f);
                            });
                            foreach (var gruppe in maBestilles.GroupBy(r => string.IsNullOrWhiteSpace(r.Component.Leverandor) ? "Uten leverandør" : r.Component.Leverandor!))
                            {
                                t.Cell().ColumnSpan(4).Background(org.AksentLys).PaddingVertical(3).PaddingHorizontal(5).Text(gruppe.Key).Bold();
                                foreach (var r in gruppe)
                                {
                                    t.Cell().BorderBottom(0.5f).BorderColor(lys).PaddingVertical(3).PaddingHorizontal(5).Text("");
                                    t.Cell().BorderBottom(0.5f).BorderColor(lys).PaddingVertical(3).PaddingHorizontal(5).Text(r.Component.Produktkode ?? "").FontColor(Colors.Grey.Darken2);
                                    t.Cell().BorderBottom(0.5f).BorderColor(lys).PaddingVertical(3).PaddingHorizontal(5).Text(r.Component.Navn);
                                    t.Cell().BorderBottom(0.5f).BorderColor(lys).PaddingVertical(3).PaddingHorizontal(5).AlignRight().Text($"{r.MaBestilles} stk").Bold();
                                }
                            }
                        });
                    }

                    col.Item().PaddingTop(28).Row(row =>
                    {
                        void Signatur(string tekst) => row.RelativeItem().PaddingRight(24).BorderTop(0.8f).BorderColor(PdfStil.Ink).PaddingTop(3)
                            .Text(tekst).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        Signatur("Plukket av");
                        Signatur("Dato");
                        Signatur("Kontrollert av");
                    });
                });

                page.Footer().BorderTop(0.5f).BorderColor(PdfStil.SandBorder).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(org.Bunntekst).FontSize(7).FontColor(Colors.Grey.Darken1);
                    row.AutoItem().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                        x.Span($"Plukkliste · {prosjekt.Navn} · side ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
