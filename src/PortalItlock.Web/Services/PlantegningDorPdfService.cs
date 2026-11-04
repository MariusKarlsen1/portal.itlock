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

public class PlantegningDorPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private const string ItlockAksent = "#2f3a45";

    private static readonly Dictionary<MontasjeStatus, string> StatusFarge = new()
    {
        [MontasjeStatus.FerdigMontert] = "#2b7a4b",
        [MontasjeStatus.Montert] = "#d9a520",
        [MontasjeStatus.IkkeStartet] = "#2f6fb3"
    };

    public async Task<byte[]?> GenerateAsync(int plantegningId)
    {
        var plantegning = await db.Plantegninger
            .Include(p => p.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(p => p.Fil)
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

        // Mal PD1: se PlantegningPdfMal.
        var org = RapportProfil.Fra(tenantContext.Current);
        const string tittel = "Dørplantegning";
        var funksjoner = dorer.SelectMany(d => d.Funksjoner)
            .Where(f => !string.IsNullOrWhiteSpace(f.Forkortelse))
            .DistinctBy(f => f.Forkortelse)
            .OrderBy(f => f.Forkortelse)
            .ToList();

        var document = Document.Create(doc =>
        {
            // PD1: side 1 er bare tegningen, side 2 er dørlisten (som forklarer
            // status og funksjoner på tegningen).
            doc.Page(page =>
            {
                PlantegningPdfMal.Side(page);
                page.Header().Element(e => PlantegningPdfMal.Topp(e, org, pdfLogo, tittel, plantegning));
                page.Content().Element(e => PlantegningPdfMal.Tegning(e, svg));
                page.Footer().Element(e => PlantegningPdfMal.Bunn(e, org, tittel, plantegning));
            });

            if (dorer.Count == 0)
            {
                return;
            }

            doc.Page(page =>
            {
                PlantegningPdfMal.Side(page);
                page.Header().Element(e => PlantegningPdfMal.Topp(e, org, pdfLogo, $"{tittel} – dørliste", plantegning));
                page.Content().Column(col =>
                {
                    PlantegningPdfMal.ListeOverskrift(col, "Dører på tegningen",
                        $"{dorer.Count} dører · {dorer.Count(d => d.Status == MontasjeStatus.FerdigMontert)} ferdig montert");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(62);
                            cd.ConstantColumn(58);
                            cd.RelativeColumn(1.4f);
                            cd.ConstantColumn(48);
                            cd.RelativeColumn(1.6f);
                            cd.ConstantColumn(105);
                            cd.ConstantColumn(78);
                        });
                        table.Header(h =>
                        {
                            foreach (var kol in new[] { "Dørnr.", "Merke", "Dør til", "Brann", "Funksjoner", "Status", "Kontrollert" })
                            {
                                h.Cell().Element(c => ListePdfStil.HodeTekst(c, kol));
                            }
                        });
                        var i = 0;
                        foreach (var d in dorer.OrderBy(d => d.Dornummer, StringComparer.OrdinalIgnoreCase))
                        {
                            var zebra = i++ % 2 == 1;
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Text(d.Dornummer).Bold();
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Text(d.Romnr ?? "");
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Text(d.DorTil ?? "");
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Text(string.IsNullOrWhiteSpace(d.Brann) ? "–" : d.Brann);
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Text(string.Join(" · ", d.Funksjoner
                                .Where(f => !string.IsNullOrWhiteSpace(f.Forkortelse)).Select(f => f.Forkortelse)));
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Row(r =>
                            {
                                r.AutoItem().AlignMiddle().Width(7).Height(7).CornerRadius(3.5f).Background(Color.FromHex(StatusFarge[d.Status]));
                                r.AutoItem().PaddingLeft(4).Text(d.Status.Visningsnavn()).FontSize(8);
                            });
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).PaddingTop(10).LineHorizontal(0.6f).LineColor(PlantegningPdfMal.Svak);
                        }
                    });

                    if (funksjoner.Count > 0)
                    {
                        col.Item().PaddingTop(6).Text($"Funksjoner: {string.Join(" · ", funksjoner.Select(f => $"{f.Forkortelse} = {f.Navn.ToLowerInvariant()}"))}")
                            .FontSize(7.5f).FontColor(PlantegningPdfMal.Dempet);
                    }
                });
                page.Footer().Element(e => PlantegningPdfMal.Bunn(e, org, tittel, plantegning));
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
