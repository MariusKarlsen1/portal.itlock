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

    public async Task<byte[]?> GenerateAsync(int tilbudId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Prosjekt)
            .Include(t => t.Linjer).ThenInclude(l => l.Component)
            .FirstOrDefaultAsync(t => t.Id == tilbudId);

        if (tilbud is null)
        {
            return null;
        }

        var linjer = tilbud.Linjer.OrderBy(l => l.Rekkefolge).ToList();
        var varekost = linjer.Sum(l => l.Innpris * l.Antall);
        var utprisVarer = linjer.Sum(l => l.Utpris * l.Antall);
        var minutter = linjer.Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
        var arbeidstidTimer = minutter / 60m;
        var kalkulertMontasjekost = Math.Round(tilbud.Timepris * arbeidstidTimer, 2);
        var montasjekost = tilbud.Montasjekost ?? kalkulertMontasjekost;
        var totalt = utprisVarer + montasjekost;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("itlock AS").FontSize(16).Bold();
                    col.Item().Text(tilbud.Tittel).FontSize(13).SemiBold();
                    if (tilbud.Prosjekt is not null)
                    {
                        col.Item().Text(tilbud.Prosjekt.Navn).FontSize(10).FontColor(Colors.Grey.Darken1);
                    }
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);

                    if (!string.IsNullOrWhiteSpace(tilbud.Forside))
                    {
                        foreach (var avsnitt in tilbud.Forside.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            col.Item().Text(avsnitt.Trim());
                        }
                        col.Item().PageBreak();
                    }

                    if (!tilbud.VisKunTotaltUtenMva)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Varekost: {FormatKr(varekost)}");
                            row.RelativeItem().Text($"Montasjekost: {FormatKr(montasjekost)}");
                            row.RelativeItem().Text($"Estimert arbeidstid: {minutter / 60}t {minutter % 60}min");
                        });
                    }

                    if (!tilbud.VisKunTotalsum && linjer.Count > 0)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                if (tilbud.VisProduktkode)
                                {
                                    columns.RelativeColumn(2);
                                }
                                columns.RelativeColumn(1);
                                if (tilbud.VisEnhetspris)
                                {
                                    columns.RelativeColumn(1.5f);
                                }
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Varenavn").Bold();
                                if (tilbud.VisProduktkode)
                                {
                                    header.Cell().Text("Produktkode").Bold();
                                }
                                header.Cell().Text("Antall").Bold();
                                if (tilbud.VisEnhetspris)
                                {
                                    header.Cell().Text("Enhetspris").Bold();
                                }
                                header.Cell().Text("Totalt").Bold();
                            });

                            foreach (var l in linjer)
                            {
                                table.Cell().Text(l.Navn);
                                if (tilbud.VisProduktkode)
                                {
                                    table.Cell().Text(l.Component?.Produktkode ?? "");
                                }
                                table.Cell().Text(l.Antall.ToString());
                                if (tilbud.VisEnhetspris)
                                {
                                    table.Cell().Text(FormatKr(l.Utpris));
                                }
                                table.Cell().Text(FormatKr(l.Utpris * l.Antall));
                            }
                        });
                    }

                    col.Item().PaddingTop(10).AlignRight().Text($"Totalt uten mva: {FormatKr(totalt)}").FontSize(13).Bold();
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

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
