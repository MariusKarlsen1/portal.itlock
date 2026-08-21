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

public class PlanUtstyrPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    public async Task<byte[]?> GenerateAsync(int plantegningId)
    {
        var plantegning = await db.Plantegninger
            .Include(p => p.Prosjekt)
            .FirstOrDefaultAsync(p => p.Id == plantegningId);

        if (plantegning is null)
        {
            return null;
        }

        var utstyr = await db.PlanUtstyr
            .Where(u => u.PlantegningId == plantegningId)
            .ToListAsync();

        var utstyrIds = utstyr.Select(u => u.Id).ToList();

        var forbindelser = await db.PlanForbindelser
            .Where(f => utstyrIds.Contains(f.FraUtstyrId))
            .ToListAsync();

        var utstyrById = utstyr.ToDictionary(u => u.Id);

        using var bitmap = SKBitmap.Decode(plantegning.Data);
        var bildeBredde = bitmap?.Width ?? 1000;
        var bildeHoyde = bitmap?.Height ?? 700;

        var svg = BuildSvg(plantegning, utstyr, forbindelser, utstyrById, bildeBredde, bildeHoyde);
        var svgHoyde = 380f;
        var svgBredde = svgHoyde * bildeBredde / bildeHoyde;

        var brukteTyper = utstyr.Select(u => u.Type).Distinct().OrderBy(t => t.Visningsnavn()).ToList();
        var brukteKabler = forbindelser.Select(f => f.Type).Distinct().OrderBy(t => t.Visningsnavn()).ToList();

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
                            headCol.Item().Text("Utstyr og kabeltrekk").FontSize(18).SemiBold();
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
                        row.RelativeItem().Column(legendCol =>
                        {
                            legendCol.Item().Text("Utstyr").Bold();
                            if (brukteTyper.Count == 0)
                            {
                                legendCol.Item().Text("Ingen utstyr plassert.").FontColor(Colors.Grey.Darken1);
                            }
                            foreach (var type in brukteTyper)
                            {
                                legendCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.ConstantItem(10).Height(10).Background(Color.FromHex(type.Farge()));
                                    r.RelativeItem().PaddingLeft(6).Text($"{type.Kode()} - {type.Visningsnavn()}");
                                });
                            }
                        });

                        row.RelativeItem().Column(legendCol =>
                        {
                            legendCol.Item().Text("Kabling").Bold();
                            if (brukteKabler.Count == 0)
                            {
                                legendCol.Item().Text("Ingen forbindelser tegnet.").FontColor(Colors.Grey.Darken1);
                            }
                            foreach (var kt in brukteKabler)
                            {
                                legendCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.ConstantItem(10).Height(10).Background(Color.FromHex(kt.Farge()));
                                    r.RelativeItem().PaddingLeft(6).Text(kt.Visningsnavn());
                                });
                            }
                        });
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

    private static string BuildSvg(
        Plantegning plantegning,
        List<PlanUtstyr> utstyr,
        List<PlanForbindelse> forbindelser,
        Dictionary<int, PlanUtstyr> utstyrById,
        int bildeBredde,
        int bildeHoyde)
    {
        var base64 = Convert.ToBase64String(plantegning.Data);
        var diagonal = Math.Sqrt((double)bildeBredde * bildeBredde + (double)bildeHoyde * bildeHoyde);
        var radius = diagonal * 0.014;
        var strekbredde = diagonal * 0.0035;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {bildeBredde} {bildeHoyde}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<image xlink:href='data:{plantegning.ContentType};base64,{base64}' href='data:{plantegning.ContentType};base64,{base64}' x='0' y='0' width='{bildeBredde}' height='{bildeHoyde}' preserveAspectRatio='none' />");

        foreach (var f in forbindelser)
        {
            if (!utstyrById.TryGetValue(f.FraUtstyrId, out var fra) || !utstyrById.TryGetValue(f.TilUtstyrId, out var til))
            {
                continue;
            }

            var x1 = (fra.PosX / 100.0) * bildeBredde;
            var y1 = (fra.PosY / 100.0) * bildeHoyde;
            var x2 = (til.PosX / 100.0) * bildeBredde;
            var y2 = (til.PosY / 100.0) * bildeHoyde;
            var dash = SkalerStrekMonster(f.Type.StrekMonster(), diagonal);
            var dashAttr = dash is null ? "" : $"stroke-dasharray='{dash}'";

            sb.Append(CultureInfo.InvariantCulture,
                $"<line x1='{Inv(x1)}' y1='{Inv(y1)}' x2='{Inv(x2)}' y2='{Inv(y2)}' stroke='{f.Type.Farge()}' stroke-width='{Inv(strekbredde)}' {dashAttr} />");
        }

        foreach (var u in utstyr)
        {
            var cx = (u.PosX / 100.0) * bildeBredde;
            var cy = (u.PosY / 100.0) * bildeHoyde;

            sb.Append(CultureInfo.InvariantCulture,
                $"<circle cx='{Inv(cx)}' cy='{Inv(cy)}' r='{Inv(radius)}' fill='{u.Type.Farge()}' stroke='white' stroke-width='{Inv(radius * 0.18)}' />");
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x='{Inv(cx)}' y='{Inv(cy + radius * 0.32)}' font-size='{Inv(radius * 0.85)}' font-family='Arial, sans-serif' font-weight='bold' fill='white' text-anchor='middle'>{u.Type.Kode()}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string? SkalerStrekMonster(string monster, double diagonal)
    {
        if (string.IsNullOrEmpty(monster))
        {
            return null;
        }

        var skala = diagonal * 0.0025;
        var deler = monster.Split(',').Select(p => Inv(double.Parse(p, CultureInfo.InvariantCulture) * skala));
        return string.Join(",", deler);
    }

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
