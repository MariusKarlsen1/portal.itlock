using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Sjekkliste for utskiftning av lås: målene til venstre og målskissen til høyre,
// slik at punktnumrene i tabellene kan leses mot tegningen.
public class LasUtskiftingPdfService(ApplicationDbContext db, PdfLogo pdfLogo, IWebHostEnvironment env)
{
    public async Task<byte[]?> GenerateAsync(int id)
    {
        var liste = await db.LasUtskiftinger.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (liste is null)
        {
            return null;
        }

        var skisse = Path.Combine(env.WebRootPath, "lasutskifting-diagram.png");
        var overflate = liste.Overflatebehandling == "Annet" && !string.IsNullOrWhiteSpace(liste.OverflatebehandlingAnnet)
            ? liste.OverflatebehandlingAnnet
            : liste.Overflatebehandling;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Utskiftning av lås", liste.Navn));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Sjekkliste", $"#{liste.Id}"),
                        ("Dato", liste.Dato?.ToString("dd.MM.yyyy")),
                        ("Felt utfylt", $"{liste.AntallUtfylt} av {LasUtskifting.AntallFelt}"));

                    col.Item().Row(row =>
                    {
                        row.Spacing(14);
                        row.RelativeItem().Column(venstre =>
                        {
                            venstre.Spacing(10);
                            Tabell(venstre, "Dør",
                                ("1A", "Total", MedEnhet(liste.TotalMm)),
                                ("1B", "Utside", MedEnhet(liste.UtsideMm)),
                                ("1C", "Innside", MedEnhet(liste.InnsideMm)),
                                ("2", "Dørkant til senter skilt", MedEnhet(liste.AvstandDorkantTilSkiltMm)),
                                ("3", "Fabrikat (lås)", liste.DorFabrikat),
                                ("3", "Nummer (lås)", liste.DorNummer),
                                ("4", "Stolpehøyde", liste.Stolpehoyde),
                                ("5", "Stolpebredde", liste.Stolpebredde),
                                ("6", "Overflatebehandling", overflate));
                            Tabell(venstre, "Karm",
                                ("7", "Fabrikat", liste.KarmFabrikat),
                                ("7", "Nummer", liste.KarmNummer),
                                ("8", "Avstand", liste.KarmAvstand),
                                ("9", "Skruer?", liste.Skruer));
                            Tabell(venstre, "Hengsling av dør",
                                ("", "Side", liste.HengslingSide),
                                ("", "Slagretning", liste.Slagretning));
                        });

                        if (File.Exists(skisse))
                        {
                            row.ConstantItem(170).Border(1).BorderColor(PdfStil.SandBorder).Padding(4).Image(skisse).FitWidth();
                        }
                    });

                    PdfStil.FriSeksjon(col, "Merknad", liste.Merknad);
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }

    private static string? MedEnhet(string? verdi) => string.IsNullOrWhiteSpace(verdi) ? null : $"{verdi} mm";

    private static void Tabell(ColumnDescriptor col, string tittel, params (string Punkt, string Felt, string? Verdi)[] rader)
    {
        col.Item().Column(inner =>
        {
            PdfStil.SeksjonTittel(inner, tittel);
            inner.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(26);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                });

                var alternerende = false;
                foreach (var (punkt, felt, verdi) in rader)
                {
                    var rad = alternerende;
                    table.Cell().Element(c => PdfStil.TabellRad(c, rad)).Text(punkt).SemiBold();
                    table.Cell().Element(c => PdfStil.TabellRad(c, rad)).Text(felt);
                    table.Cell().Element(c => PdfStil.TabellRad(c, rad)).Text(string.IsNullOrWhiteSpace(verdi) ? "–" : verdi);
                    alternerende = !alternerende;
                }
            });
        });
    }
}
