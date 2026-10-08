using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class DorpakkePdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<byte[]?> GenerateAsync(int pakkeId)
    {
        var pakke = await db.Packages
            .Include(p => p.Krav).ThenInclude(k => k.RequirementValue).ThenInclude(v => v!.Dimensjon)
            .Include(p => p.Komponenter).ThenInclude(k => k.Component).ThenInclude(c => c!.Type)
            .FirstOrDefaultAsync(p => p.Id == pakkeId);

        if (pakke is null)
        {
            return null;
        }

        var grunndata = PakkeVisningHjelper.Grunndata(pakke);

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, pakke.Navn, "Dørpakke"));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(12);

                    PdfStil.FriSeksjon(col, "Beskrivelse", pakke.Beskrivelse);
                    PdfStil.FriSeksjon(col, "Bruksområde", pakke.Bruksomrade);

                    if (grunndata.Count > 0)
                    {
                        PdfStil.SeksjonTittel(col, "Grunndata");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2f);
                                c.RelativeColumn(2f);
                            });

                            var i = 0;
                            foreach (var rad in grunndata)
                            {
                                var alt = i++ % 2 == 1;
                                IContainer Celle() => PdfStil.TabellRad(table.Cell(), alt);
                                Celle().Text(rad.Label).FontSize(8.5f);
                                Celle().Text(rad.Verdi).FontSize(8.5f).SemiBold();
                            }
                        });
                    }

                    if (pakke.Komponenter.Count > 0)
                    {
                        PdfStil.SeksjonTittel(col, $"Komponenter ({pakke.Komponenter.Count})");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.6f);
                                c.RelativeColumn(2.6f);
                                c.RelativeColumn(1f);
                            });

                            table.Header(header =>
                            {
                                IContainer Hode() => PdfStil.TabellHode(header.Cell());
                                Hode().Text("Type").FontSize(8).Bold().FontColor(Colors.White);
                                Hode().Text("Navn").FontSize(8).Bold().FontColor(Colors.White);
                                Hode().Text("Antall").FontSize(8).Bold().FontColor(Colors.White);
                            });

                            var j = 0;
                            foreach (var k in pakke.Komponenter.OrderBy(k => k.Component?.Navn))
                            {
                                var alt = j++ % 2 == 1;
                                IContainer Rad() => PdfStil.TabellRad(table.Cell(), alt);
                                Rad().Text(k.Component?.Type?.Navn ?? "").FontSize(8.5f);
                                Rad().Text(k.Component?.Navn ?? "").FontSize(8.5f);
                                Rad().Text(k.Antall.ToString()).FontSize(8.5f);
                            }
                        });
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
