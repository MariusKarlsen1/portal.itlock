using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class TimeoversiktService(ApplicationDbContext db)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public async Task<List<Timeregistrering>> HentRegistreringerAsync(DateTime fra, DateTime til, int? montorId)
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

    public byte[] GenererPdf(List<Timeregistrering> registreringer, DateTime fra, DateTime til, string? montorNavn)
    {
        var sammendrag = registreringer
            .GroupBy(t => t.Montor?.Navn ?? "-")
            .Select(g => new MontorSum
            {
                MontorNavn = g.Key,
                Normal = g.Where(t => t.Type == TimeregistreringType.NormalArbeidstid).Sum(t => t.TotalTimer),
                Overtid50 = g.Where(t => t.Type == TimeregistreringType.Overtid50).Sum(t => t.TotalTimer),
                Overtid100 = g.Where(t => t.Type == TimeregistreringType.Overtid100).Sum(t => t.TotalTimer),
                Avspasering = g.Where(t => t.Type == TimeregistreringType.Avspasering).Sum(t => t.TotalTimer)
            })
            .OrderBy(m => m.MontorNavn)
            .ToList();

        var totalt = registreringer.Sum(t => t.TotalTimer);

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
                    col.Item().Text("Timeoversikt").FontSize(13).SemiBold();
                    col.Item().Text($"{fra:dd.MM.yyyy} - {til:dd.MM.yyyy}" + (montorNavn is null ? "" : $" · {montorNavn}"))
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.3f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Montør").Bold();
                            header.Cell().Text("Normal").Bold();
                            header.Cell().Text("50% ot.").Bold();
                            header.Cell().Text("100% ot.").Bold();
                            header.Cell().Text("Avspas.").Bold();
                            header.Cell().Text("Totalt").Bold();
                        });

                        foreach (var m in sammendrag)
                        {
                            table.Cell().Text(m.MontorNavn);
                            table.Cell().Text(FormatTimer(m.Normal));
                            table.Cell().Text(FormatTimer(m.Overtid50));
                            table.Cell().Text(FormatTimer(m.Overtid100));
                            table.Cell().Text(FormatTimer(m.Avspasering));
                            table.Cell().Text(FormatTimer(m.Totalt)).Bold();
                        }
                    });

                    col.Item().AlignRight().Text($"Totalt alle: {FormatTimer(totalt)}").FontSize(12).Bold();

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Dato").Bold();
                            header.Cell().Text("Montør").Bold();
                            header.Cell().Text("Type").Bold();
                            header.Cell().Text("Timer").Bold();
                            header.Cell().Text("Arbeidsordre").Bold();
                        });

                        foreach (var t in registreringer)
                        {
                            table.Cell().Text(t.Dato.ToString("dd.MM.yyyy"));
                            table.Cell().Text(t.Montor?.Navn ?? "-");
                            table.Cell().Text(t.Type.Visningsnavn());
                            table.Cell().Text(FormatTimer(t.TotalTimer));
                            table.Cell().Text(t.Arbeidsordre?.Tittel ?? "-");
                        }
                    });
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

    private static string FormatTimer(decimal value) => value.ToString("N1", Kultur) + " t";

    private sealed class MontorSum
    {
        public string MontorNavn { get; set; } = "";
        public decimal Normal { get; set; }
        public decimal Overtid50 { get; set; }
        public decimal Overtid100 { get; set; }
        public decimal Avspasering { get; set; }
        public decimal Totalt => Normal + Overtid50 + Overtid100 + Avspasering;
    }
}
