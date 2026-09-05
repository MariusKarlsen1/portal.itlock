using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace PortalItlock.Web.Services;

public class PlantegningDorPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";
    private const string ItlockAksent = "#835e41";

    private static readonly Dictionary<MontasjeStatus, string> StatusFarge = new()
    {
        [MontasjeStatus.FerdigMontert] = "#2b7a4b",
        [MontasjeStatus.Montert] = "#d9a520",
        [MontasjeStatus.IkkeStartet] = "#2f6fb3"
    };

    public async Task<byte[]?> GenerateAsync(int plantegningId)
    {
        var plantegning = await db.Plantegninger
            .Include(p => p.Prosjekt)
            .FirstOrDefaultAsync(p => p.Id == plantegningId);

        if (plantegning is null)
        {
            return null;
        }

        var dorer = await db.Dorer
            .Include(d => d.Funksjoner)
            .Where(d => d.PlantegningId == plantegningId && d.PosX != null && d.PosY != null)
            .ToListAsync();

        using var bitmap = SKBitmap.Decode(plantegning.Data);
        var bildeBredde = bitmap?.Width ?? 1000;
        var bildeHoyde = bitmap?.Height ?? 700;

        var svg = BuildSvg(plantegning, dorer, bildeBredde, bildeHoyde);
        var svgHoyde = 380f;
        var svgBredde = svgHoyde * bildeBredde / bildeHoyde;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(headCol =>
                        {
                            headCol.Item().Text("Dørplantegning").FontSize(18).SemiBold();
                            headCol.Item().Text(plantegning.Navn).FontSize(11).FontColor(Colors.Grey.Darken1);
                        });
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text(t =>
                    {
                        t.Span("Prosjekt: ").Bold();
                        t.Span(plantegning.Prosjekt?.Navn ?? "");
                        t.Span("     Dato: ").Bold();
                        t.Span(DateTime.Now.ToString("dd.MM.yyyy"));
                    });

                    col.Item().AlignCenter().Width(svgBredde).Height(svgHoyde).Svg(svg).FitArea();

                    col.Item().Row(row =>
                    {
                        foreach (var status in new[] { MontasjeStatus.FerdigMontert, MontasjeStatus.Montert, MontasjeStatus.IkkeStartet })
                        {
                            row.RelativeItem().Row(r =>
                            {
                                r.ConstantItem(10).Height(10).Background(Color.FromHex(StatusFarge[status]));
                                r.RelativeItem().PaddingLeft(6).Text(status.Visningsnavn());
                            });
                        }
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Column(c =>
                {
                    c.Item().AlignCenter().Text($"{FirmaNavn} - {FirmaAdresse} - Tlf {FirmaTelefon} - {FirmaEpost}").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string BuildSvg(Plantegning plantegning, List<Dor> dorer, int bildeBredde, int bildeHoyde)
    {
        var base64 = Convert.ToBase64String(plantegning.Data);
        var diagonal = Math.Sqrt((double)bildeBredde * bildeBredde + (double)bildeHoyde * bildeHoyde);
        var radius = diagonal * 0.0055;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {bildeBredde} {bildeHoyde}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<image xlink:href='data:{plantegning.ContentType};base64,{base64}' href='data:{plantegning.ContentType};base64,{base64}' x='0' y='0' width='{bildeBredde}' height='{bildeHoyde}' preserveAspectRatio='none' />");

        var fontSize = radius * 0.95;

        foreach (var d in dorer)
        {
            var cx = (d.PosX!.Value / 100.0) * bildeBredde;
            var cy = (d.PosY!.Value / 100.0) * bildeHoyde;
            var farge = StatusFarge[d.Status];
            var label = string.IsNullOrWhiteSpace(d.Romnr) ? d.Dornummer : d.Romnr;
            var funksjoner = string.Join(", ", d.Funksjoner
                .Where(f => !string.IsNullOrWhiteSpace(f.Forkortelse))
                .Select(f => f.Forkortelse));

            // Dørnummer/romnr over prikken.
            AppendSentrertLabel(sb, cx, cy - radius - fontSize * 0.65, label, fontSize, "#292927", "white", "#00000026");

            sb.Append(CultureInfo.InvariantCulture,
                $"<circle cx='{Inv(cx)}' cy='{Inv(cy)}' r='{Inv(radius)}' fill='{farge}' stroke='white' stroke-width='{Inv(radius * 0.22)}' />");

            // Dørfunksjoner under prikken.
            if (!string.IsNullOrEmpty(funksjoner))
            {
                AppendSentrertLabel(sb, cx, cy + radius + fontSize * 1.35, funksjoner, fontSize * 0.95, "white", ItlockAksent, null);
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendSentrertLabel(StringBuilder sb, double cx, double baselineY, string tekst, double fontSize, string tekstfarge, string bakgrunnsfarge, string? kantfarge)
    {
        if (string.IsNullOrEmpty(tekst))
        {
            return;
        }

        var bredde = tekst.Length * fontSize * 0.62 + fontSize * 0.6;
        var hoyde = fontSize * 1.4;
        var kantAttr = kantfarge is null ? "" : $"stroke='{kantfarge}' stroke-width='{Inv(fontSize * 0.06)}'";

        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x='{Inv(cx - bredde / 2)}' y='{Inv(baselineY - fontSize * 1.05)}' width='{Inv(bredde)}' height='{Inv(hoyde)}' fill='{bakgrunnsfarge}' {kantAttr} />");
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x='{Inv(cx)}' y='{Inv(baselineY)}' font-size='{Inv(fontSize)}' font-family='Arial, sans-serif' font-weight='bold' fill='{tekstfarge}' text-anchor='middle'>{System.Net.WebUtility.HtmlEncode(tekst)}</text>");
    }

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
