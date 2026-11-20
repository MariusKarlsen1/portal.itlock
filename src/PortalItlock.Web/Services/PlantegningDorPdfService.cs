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
                            table.Cell().Element(c => ListePdfStil.Rad(c, zebra)).Text(string.Join(" · ", d.Funksjoner.IFastRekkefolge()
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
        var funkFont = fontSize * 0.95;

        // Plassering uten overlapp: hver dør får én etikett (dørnummer øverst,
        // funksjonene under). Den legges på nærmeste ledige plass rundt prikken –
        // først rett over, så under, til siden og skrått, stadig lenger ut. Er den
        // flyttet unna prikken, går en tynn strek til prikken. Prikkene tegnes til
        // slutt, så de aldri blir dekket.
        var punkter = dorer.Select(d =>
        {
            var funksjoner = string.Join(", ", d.Funksjoner.IFastRekkefolge()
                .Where(f => !string.IsNullOrWhiteSpace(f.Forkortelse))
                .Select(f => f.Forkortelse));
            var navn = string.IsNullOrWhiteSpace(d.Romnr) ? d.Dornummer : d.Romnr!;
            var wNavn = TekstBredde(navn, fontSize);
            var wFunk = string.IsNullOrEmpty(funksjoner) ? 0 : TekstBredde(funksjoner, funkFont);
            var hNavn = fontSize * 1.4;
            var hFunk = string.IsNullOrEmpty(funksjoner) ? 0 : funkFont * 1.4;
            return new Etikett
            {
                Cx = d.PosX!.Value / 100.0 * bildeBredde,
                Cy = d.PosY!.Value / 100.0 * bildeHoyde,
                Farge = StatusFarge[d.Status],
                Navn = navn,
                Funksjoner = funksjoner,
                WNavn = wNavn,
                WFunk = wFunk,
                HNavn = hNavn,
                HFunk = hFunk,
                W = Math.Max(wNavn, wFunk),
                H = hNavn + hFunk
            };
        }).ToList();

        var luft = radius * 0.35;
        var plassert = new List<(double X, double Y, double W, double H)>();
        double Overlapp((double X, double Y, double W, double H) r)
        {
            double sum = 0;
            foreach (var o in plassert)
            {
                var ox = Math.Min(r.X + r.W, o.X + o.W) - Math.Max(r.X, o.X) + luft;
                var oy = Math.Min(r.Y + r.H, o.Y + o.H) - Math.Max(r.Y, o.Y) + luft;
                if (ox > 0 && oy > 0) sum += ox * oy;
            }
            foreach (var p in punkter)
            {
                var ox = Math.Min(r.X + r.W, p.Cx + radius) - Math.Max(r.X, p.Cx - radius) + luft;
                var oy = Math.Min(r.Y + r.H, p.Cy + radius) - Math.Max(r.Y, p.Cy - radius) + luft;
                if (ox > 0 && oy > 0) sum += ox * oy * 4;
            }
            // Utenfor tegningen teller også som overlapp.
            if (r.X < 0) sum += -r.X * r.H;
            if (r.Y < 0) sum += -r.Y * r.W;
            if (r.X + r.W > bildeBredde) sum += (r.X + r.W - bildeBredde) * r.H;
            if (r.Y + r.H > bildeHoyde) sum += (r.Y + r.H - bildeHoyde) * r.W;
            return sum;
        }

        // Tette områder først, så dørene med flest naboer får de beste plassene.
        var rekkefolge = punkter.OrderByDescending(p => punkter.Count(q => Math.Abs(q.Cx - p.Cx) < p.W * 1.5 && Math.Abs(q.Cy - p.Cy) < p.H * 2.5)).ToList();
        var retninger = new (double Dx, double Dy)[] { (0, -1), (0, 1), (1, 0), (-1, 0), (1, -1), (-1, -1), (1, 1), (-1, 1) };
        foreach (var e in rekkefolge)
        {
            (double X, double Y, double W, double H) best = default;
            var bestScore = double.MaxValue;
            var bestRing = 0;
            for (var ring = 0; ring < 18 && bestScore > 0; ring++)
            {
                var avstand = radius * 1.25 + ring * Math.Max(e.H, radius * 2) * 0.75;
                foreach (var (dx, dy) in retninger)
                {
                    // Nærmeste kant av etiketten ligger «avstand» fra midten av prikken.
                    var x = dx == 0 ? e.Cx - e.W / 2 : e.Cx + dx * (avstand + e.W / 2) - e.W / 2;
                    var y = dy == 0 ? e.Cy - e.H / 2 : e.Cy + dy * (avstand + e.H / 2) - e.H / 2;
                    var rekt = (x, y, e.W, e.H);
                    var overlapp = Overlapp(rekt);
                    if (overlapp + ring * 0.0001 < bestScore)
                    {
                        bestScore = overlapp + ring * 0.0001;
                        best = rekt;
                        bestRing = ring;
                    }
                    if (overlapp <= 0)
                    {
                        bestScore = 0;
                        break;
                    }
                }
            }
            e.X = best.X;
            e.Y = best.Y;
            e.Strek = bestRing > 0;
            plassert.Add(best);
        }

        // 1) streker fra prikk til etikett som er flyttet unna
        foreach (var e in punkter.Where(e => e.Strek))
        {
            var tx = Math.Clamp(e.Cx, e.X, e.X + e.W);
            var ty = Math.Clamp(e.Cy, e.Y, e.Y + e.H);
            sb.Append(CultureInfo.InvariantCulture,
                $"<line x1='{Inv(e.Cx)}' y1='{Inv(e.Cy)}' x2='{Inv(tx)}' y2='{Inv(ty)}' stroke='{ItlockAksent}' stroke-width='{Inv(radius * 0.18)}' stroke-opacity='0.75' />");
        }

        // 2) etikettene
        foreach (var e in punkter)
        {
            var navnX = e.X + (e.W - e.WNavn) / 2;
            sb.Append(CultureInfo.InvariantCulture,
                $"<rect x='{Inv(navnX)}' y='{Inv(e.Y)}' width='{Inv(e.WNavn)}' height='{Inv(e.HNavn)}' fill='white' stroke='#00000040' stroke-width='{Inv(fontSize * 0.06)}' />");
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x='{Inv(e.X + e.W / 2)}' y='{Inv(e.Y + e.HNavn - fontSize * 0.36)}' font-size='{Inv(fontSize)}' font-family='Arial, sans-serif' font-weight='bold' fill='#292927' text-anchor='middle'>{System.Net.WebUtility.HtmlEncode(e.Navn)}</text>");
            if (!string.IsNullOrEmpty(e.Funksjoner))
            {
                var fx = e.X + (e.W - e.WFunk) / 2;
                var fy = e.Y + e.HNavn;
                sb.Append(CultureInfo.InvariantCulture,
                    $"<rect x='{Inv(fx)}' y='{Inv(fy)}' width='{Inv(e.WFunk)}' height='{Inv(e.HFunk)}' fill='{ItlockAksent}' />");
                sb.Append(CultureInfo.InvariantCulture,
                    $"<text x='{Inv(e.X + e.W / 2)}' y='{Inv(fy + e.HFunk - funkFont * 0.36)}' font-size='{Inv(funkFont)}' font-family='Arial, sans-serif' font-weight='bold' fill='white' text-anchor='middle'>{System.Net.WebUtility.HtmlEncode(e.Funksjoner)}</text>");
            }
        }

        // 3) prikkene øverst
        foreach (var e in punkter)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"<circle cx='{Inv(e.Cx)}' cy='{Inv(e.Cy)}' r='{Inv(radius)}' fill='{e.Farge}' stroke='white' stroke-width='{Inv(radius * 0.22)}' />");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private sealed class Etikett
    {
        public double Cx { get; init; }
        public double Cy { get; init; }
        public required string Farge { get; init; }
        public required string Navn { get; init; }
        public required string Funksjoner { get; init; }
        public double WNavn { get; init; }
        public double WFunk { get; init; }
        public double HNavn { get; init; }
        public double HFunk { get; init; }
        public double W { get; init; }
        public double H { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool Strek { get; set; }
    }

    private static double TekstBredde(string tekst, double fontSize) => tekst.Length * fontSize * 0.62 + fontSize * 0.6;

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
