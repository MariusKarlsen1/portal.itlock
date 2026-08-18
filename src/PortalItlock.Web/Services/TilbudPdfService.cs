using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class TilbudPdfService(ApplicationDbContext db)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";
    private const string FirmaKontaktperson = "Marius Karlsen";
    private const decimal MvaSats = 0.25m;

    public async Task<byte[]?> GenerateAsync(int tilbudId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Prosjekt)
            .Include(t => t.Linjer).ThenInclude(l => l.Component).ThenInclude(c => c!.Type)
            .FirstOrDefaultAsync(t => t.Id == tilbudId);

        if (tilbud is null)
        {
            return null;
        }

        var linjer = tilbud.Linjer.OrderBy(l => l.Rekkefolge).ToList();
        var utprisVarer = linjer.Sum(l => l.Utpris * l.Antall);
        var minutter = linjer.Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
        var arbeidstidTimer = minutter / 60m;
        var kalkulertMontasjekost = Math.Round(tilbud.Timepris * arbeidstidTimer, 2);
        var montasjekost = tilbud.Montasjekost ?? kalkulertMontasjekost;
        var totaltUtenMva = utprisVarer + montasjekost;

        var overskrift = tilbud.Prosjekt is not null ? $"{tilbud.Prosjekt.Navn} - {tilbud.Tittel}" : tilbud.Tittel;

        var document = Document.Create(doc =>
        {
            // Forside
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(140).AlignCenter().Text("Tilbud").FontSize(34).SemiBold();
                    col.Item().PaddingTop(8).AlignCenter().Text(overskrift).FontSize(17).FontColor(Colors.Grey.Darken2);
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("Prosjekt: ").Bold();
                            t.Span(tilbud.Prosjekt?.Navn ?? "");
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Kontaktperson: ").Bold();
                            t.Span(FirmaKontaktperson);
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Dato: ").Bold();
                            t.Span(tilbud.OpprettetDato.ToString("dd.MM.yyyy"));
                        });
                    });
                    row.RelativeItem(1);
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().AlignRight().Text(t => RenderLogoSpans(t, 14));
                        c.Item().AlignRight().Text(FirmaAdresse).FontSize(8);
                        c.Item().AlignRight().Text($"Telefon {FirmaTelefon}").FontSize(8);
                        c.Item().AlignRight().Text($"Epost {FirmaEpost}").FontSize(8);
                    });
                });
            });

            // Forbehold + produktsammendrag + totalsum
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(t => RenderLogoSpans(t, 13));
                        row.RelativeItem().AlignRight().Text(overskrift).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(tilbud.Forside))
                    {
                        RenderForside(col, tilbud.Forside);
                        col.Item().PageBreak();
                    }

                    if (!tilbud.VisKunTotalsum && linjer.Count > 0)
                    {
                        col.Item().Text("Produktsammendrag").FontSize(16).SemiBold();

                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(3f);
                                columns.RelativeColumn(2f);
                                if (tilbud.VisProduktkode)
                                {
                                    columns.RelativeColumn(1.5f);
                                }
                                columns.RelativeColumn(1f);
                                if (tilbud.VisEnhetspris)
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                }
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Varenavn").Bold();
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Varenavn 2").Bold();
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Beslagstype").Bold();
                                if (tilbud.VisProduktkode)
                                {
                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Produktkode").Bold();
                                }
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Ant.").Bold();
                                if (tilbud.VisEnhetspris)
                                {
                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Á-pris").Bold();
                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Totalt").Bold();
                                }
                            });

                            foreach (var l in linjer)
                            {
                                table.Cell().Text(l.Navn);
                                table.Cell().Text(l.Component?.Beskrivelse ?? "");
                                table.Cell().Text(l.Component?.Type?.Navn ?? "");
                                if (tilbud.VisProduktkode)
                                {
                                    table.Cell().Text(l.Component?.Produktkode ?? "");
                                }
                                table.Cell().Text(l.Antall.ToString());
                                if (tilbud.VisEnhetspris)
                                {
                                    table.Cell().Text(FormatKr(l.Utpris));
                                    table.Cell().Text(FormatKr(l.Utpris * l.Antall));
                                }
                            }
                        });
                    }

                    col.Item().PaddingTop(20).AlignRight().Width(260).Element(container => RenderTotalsBox(container, tilbud.VisKunTotaltUtenMva, totaltUtenMva));
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void RenderLogoSpans(TextDescriptor t, float size)
    {
        t.Span("itl").FontColor("#292927").Bold().FontSize(size);
        t.Span("o").FontColor("#835e41").Bold().FontSize(size);
        t.Span("ck").FontColor("#292927").Bold().FontSize(size);
        t.Span(" AS").FontColor("#292927").Bold().FontSize(size);
    }

    private static void RenderForside(ColumnDescriptor col, string tekst)
    {
        var paragraph = new List<string>();

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            col.Item().PaddingBottom(4).Text(string.Join("\n", paragraph)).LineHeight(1.35f);
            paragraph.Clear();
        }

        foreach (var raw in tekst.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("## "))
            {
                FlushParagraph();
                col.Item().PaddingTop(4).Text(line[3..].Trim()).FontSize(11).Bold();
            }
            else if (line.StartsWith("# "))
            {
                FlushParagraph();
                col.Item().PaddingTop(8).Text(line[2..].Trim()).FontSize(19).SemiBold();
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
            }
            else
            {
                paragraph.Add(line.Trim());
            }
        }

        FlushParagraph();
    }

    private void RenderTotalsBox(IContainer container, bool visKunTotaltUtenMva, decimal totaltUtenMva)
    {
        if (visKunTotaltUtenMva)
        {
            container.AlignRight().Text($"Totalt uten mva: {FormatKr(totaltUtenMva)}").FontSize(13).Bold();
            return;
        }

        var mva = Math.Round(totaltUtenMva * MvaSats, 2);
        var totaltInklMva = totaltUtenMva + mva;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });

            void Rad(string label, decimal verdi, bool bold)
            {
                var labelText = table.Cell().PaddingVertical(3).Text(label);
                var verdiText = table.Cell().PaddingVertical(3).AlignRight().Text(FormatKr(verdi));
                if (bold)
                {
                    labelText.Bold();
                    verdiText.Bold();
                }
            }

            table.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
            Rad("Totalt uten MVA", totaltUtenMva, false);
            Rad("MVA", mva, false);
            table.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
            Rad("Totalt inkl. MVA", totaltInklMva, true);
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
