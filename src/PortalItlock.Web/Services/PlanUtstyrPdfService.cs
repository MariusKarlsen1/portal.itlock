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

public class PlanUtstyrPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{

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
        var brukteTyper = utstyr.GroupBy(u => u.Type).OrderBy(g => g.Key.Visningsnavn()).ToList();
        var brukteKabler = forbindelser.Select(f => f.Type).Distinct().OrderBy(t => t.Visningsnavn()).ToList();

        // Mal U1: se PlantegningPdfMal. Forklaringen viser utstyret som er brukt
        // (med antall) og kabeltypene med samme strek som på tegningen.
        var org = RapportProfil.Fra(tenantContext.Current);
        const string tittel = "Utstyr og kabeltrekk";

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                PlantegningPdfMal.Side(page);
                page.Header().Element(e => PlantegningPdfMal.Topp(e, org, pdfLogo, tittel, plantegning));
                page.Content().Element(e => PlantegningPdfMal.Tegning(e, svg));
                page.Footer().Element(e => PlantegningPdfMal.Bunn(e, org, tittel, plantegning, col =>
                {
                    if (brukteTyper.Count == 0)
                    {
                        col.Item().Text("Ingen utstyr plassert.").FontSize(7.5f).FontColor(PlantegningPdfMal.Dempet);
                    }
                    else
                    {
                        PlantegningPdfMal.ForklaringRad(col, brukteTyper.Select(g => (Action<IContainer>)(c => c.Row(r =>
                        {
                            r.AutoItem().AlignMiddle().MinWidth(15).Height(9).CornerRadius(2).Background(Color.FromHex(g.Key.Farge()))
                                .PaddingHorizontal(2).AlignCenter().AlignMiddle().Text(g.Key.Kode()).FontSize(5).Bold().FontColor(Colors.White);
                            r.AutoItem().PaddingLeft(4).Text(t =>
                            {
                                t.Span(g.Key.Visningsnavn()).FontSize(7.5f);
                                t.Span($"  {g.Count()}").FontSize(7.5f).Bold();
                            });
                        }))));
                    }

                    if (brukteKabler.Count > 0)
                    {
                        col.Item().PaddingTop(3).Column(kabelCol => PlantegningPdfMal.ForklaringRad(kabelCol, brukteKabler.Select(kt => (Action<IContainer>)(c => c.Row(r =>
                        {
                            var strek = kt.StrekMonster();
                            var dash = string.IsNullOrEmpty(strek) ? "" : $" stroke-dasharray='{strek}'";
                            r.AutoItem().AlignMiddle().Width(20).Height(6)
                                .Svg($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 6'><line x1='0' y1='3' x2='20' y2='3' stroke='{kt.Farge()}' stroke-width='1.6'{dash} /></svg>");
                            r.AutoItem().PaddingLeft(4).Text(kt.Visningsnavn()).FontSize(7.5f);
                        })))));
                    }
                }));
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
        var strekbredde = diagonal * 0.0035;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {bildeBredde} {bildeHoyde}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<image xlink:href='data:{plantegning.ContentType};base64,{base64}' href='data:{plantegning.ContentType};base64,{base64}' x='0' y='0' width='{bildeBredde}' height='{bildeHoyde}' preserveAspectRatio='none' />");

        var ikonStorrelse = diagonal * 0.02;
        var fontSize = diagonal * 0.0062;

        double Px(double pst, bool x) => pst / 100.0 * (x ? bildeBredde : bildeHoyde);

        // Kabler først (under symbolene), som myke kurver gjennom knekkpunktene -
        // samme kurve som på skjermen (KabelSti). En hvit kant under streken
        // gjør den lett å se også over mørke linjer i tegningen.
        foreach (var f in forbindelser)
        {
            if (!utstyrById.TryGetValue(f.FraUtstyrId, out var fra) || !utstyrById.TryGetValue(f.TilUtstyrId, out var til))
            {
                continue;
            }

            var punkter = new List<(double X, double Y)> { (Px(fra.PosX, true), Px(fra.PosY, false)) };
            punkter.AddRange(KabelSti.Les(f.Knekkpunkter).Select(k => (Px(k.X, true), Px(k.Y, false))));
            punkter.Add((Px(til.PosX, true), Px(til.PosY, false)));
            var d = KabelSti.Kurve(punkter);
            var dash = SkalerStrekMonster(f.Type.StrekMonster(), diagonal);
            var dashAttr = dash is null ? "" : $"stroke-dasharray='{dash}'";

            sb.Append(CultureInfo.InvariantCulture,
                $"<path d='{d}' fill='none' stroke='white' stroke-opacity='0.85' stroke-width='{Inv(strekbredde * 2.4)}' stroke-linecap='round' stroke-linejoin='round' />");
            sb.Append(CultureInfo.InvariantCulture,
                $"<path d='{d}' fill='none' stroke='{f.Type.Farge()}' stroke-width='{Inv(strekbredde)}' stroke-linecap='round' stroke-linejoin='round' {dashAttr} />");
        }

        // Symbolene i riktig størrelse (Bredde/Hoyde per utstyr), med en hvit
        // glorie bak for kontrast mot tegningen.
        var opptatt = new List<(double X, double Y, double W, double H)>();
        var ikoner = new List<(PlanUtstyr U, double Cx, double Cy, double W, double H)>();
        foreach (var u in utstyr)
        {
            var cx = Px(u.PosX, true);
            var cy = Px(u.PosY, false);
            var w = ikonStorrelse * (u.Bredde > 0 ? u.Bredde : 1);
            var h = ikonStorrelse * (u.Hoyde > 0 ? u.Hoyde : 1);
            ikoner.Add((u, cx, cy, w, h));
            opptatt.Add((cx - w / 2, cy - h / 2, w, h));

            sb.Append(CultureInfo.InvariantCulture,
                $"<ellipse cx='{Inv(cx)}' cy='{Inv(cy)}' rx='{Inv(w * 0.62)}' ry='{Inv(h * 0.62)}' fill='white' opacity='0.85' />");
            var ikonSvg = u.Type.IkonSvg().Replace("currentColor", u.Type.Farge());
            sb.Append(CultureInfo.InvariantCulture,
                $"<g transform='translate({Inv(cx - w / 2)},{Inv(cy - h / 2)}) scale({Inv(w / 24.0)},{Inv(h / 24.0)})' stroke-linecap='round' stroke-linejoin='round'>{ikonSvg}</g>");
        }

        // Kodene plasseres der de ikke dekker andre symboler eller koder: under,
        // over, til høyre eller til venstre for symbolet - første ledige plass.
        foreach (var (u, cx, cy, w, h) in ikoner)
        {
            var tekst = u.Type.Kode();
            var lb = tekst.Length * fontSize * 0.62 + fontSize * 0.6;
            var lh = fontSize * 1.4;
            var luft = fontSize * 0.25;
            var kandidater = new[]
            {
                (X: cx - lb / 2, Y: cy + h / 2 + luft),
                (X: cx - lb / 2, Y: cy - h / 2 - luft - lh),
                (X: cx + w / 2 + luft, Y: cy - lh / 2),
                (X: cx - w / 2 - luft - lb, Y: cy - lh / 2),
                (X: cx + w / 2 + luft, Y: cy + h / 2),
                (X: cx - w / 2 - luft - lb, Y: cy + h / 2),
                (X: cx + w / 2 + luft, Y: cy - h / 2 - lh),
                (X: cx - w / 2 - luft - lb, Y: cy - h / 2 - lh)
            };
            var valgt = kandidater
                .Select(k => (k.X, k.Y, Overlapp: opptatt.Sum(o => Overlapp(k.X, k.Y, lb, lh, o)), Utenfor: k.X < 0 || k.Y < 0 || k.X + lb > bildeBredde || k.Y + lh > bildeHoyde))
                .OrderBy(k => k.Utenfor)
                .ThenBy(k => k.Overlapp)
                .First();
            opptatt.Add((valgt.X, valgt.Y, lb, lh));
            AppendLabel(sb, valgt.X, valgt.Y, lb, lh, tekst, fontSize);
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static double Overlapp(double x, double y, double w, double h, (double X, double Y, double W, double H) o)
    {
        var ox = Math.Max(0, Math.Min(x + w, o.X + o.W) - Math.Max(x, o.X));
        var oy = Math.Max(0, Math.Min(y + h, o.Y + o.H) - Math.Max(y, o.Y));
        return ox * oy;
    }

    private static void AppendLabel(StringBuilder sb, double x, double y, double w, double h, string tekst, double fontSize)
    {
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x='{Inv(x)}' y='{Inv(y)}' width='{Inv(w)}' height='{Inv(h)}' rx='{Inv(fontSize * 0.2)}' fill='white' stroke='#00000033' stroke-width='{Inv(fontSize * 0.06)}' />");
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x='{Inv(x + w / 2)}' y='{Inv(y + h * 0.75)}' font-size='{Inv(fontSize)}' font-family='Arial, sans-serif' font-weight='bold' fill='#292927' text-anchor='middle'>{System.Net.WebUtility.HtmlEncode(tekst)}</text>");
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
