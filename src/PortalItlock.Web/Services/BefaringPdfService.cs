using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class BefaringPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    public async Task<byte[]?> GenerateAsync(int befaringId)
    {
        var befaring = await db.Befaringer
            .Include(b => b.Dorfelt).ThenInclude(d => d.Lassystemer)
            .Include(b => b.Dorfelt).ThenInclude(d => d.Bilder)
            .FirstOrDefaultAsync(b => b.Id == befaringId);

        if (befaring is null)
        {
            return null;
        }

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(befaring.Navn).FontSize(18).SemiBold();
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(2).Text("Befaringsrapport").FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(2);

                    Seksjon(col, "Kontaktinformasjon",
                        ("Kundenr./navn", befaring.Kundenavn),
                        ("Bygg/prosjekt", befaring.Bygg),
                        ("Adresse", befaring.Adresse),
                        ("Post nr / sted", befaring.Sted),
                        ("Kontaktperson", befaring.Kontaktperson),
                        ("Tlf/mobil", befaring.Tlf),
                        ("E-post", befaring.Epost),
                        ("System nr", befaring.SystemNr),
                        ("Befart av", befaring.BefartAv),
                        ("Dato", befaring.Dato?.ToString("dd.MM.yyyy")));

                    if (!string.IsNullOrWhiteSpace(befaring.Oppdrag))
                    {
                        col.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("Oppdrag: ").SemiBold();
                            t.Span(befaring.Oppdrag);
                        });
                    }

                    foreach (var d in befaring.Dorfelt)
                    {
                        var tittel = string.IsNullOrWhiteSpace(d.Dornavn) ? "Dørfelt" : d.Dornavn;
                        if (!string.IsNullOrWhiteSpace(d.Dornr))
                        {
                            tittel += $" ({d.Dornr})";
                        }

                        col.Item().PaddingTop(16).Text(tittel).FontSize(12).SemiBold();
                        col.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        Seksjon(col, "Identifikasjon",
                            ("Dør nr", d.Dornr), ("Dørnavn", d.Dornavn), ("Dørtype", d.Dortype), ("Fløyer", d.Floyer),
                            ("BxH", d.BxH), ("Slagretning", d.Slagretning), ("Låssystemnr", d.Lassystemnr));

                        Seksjon(col, "Krav",
                            ("Brannkrav", d.Brannkrav), ("Brannklasse", d.Brannklasse), ("FG/Beskyttelsesklasse", d.Fg),
                            ("Sikringsklasse", d.Sikringsklasse), ("Risikoklasse", d.Risikoklasse),
                            ("Universell utforming", d.UniversellUtforming), ("Åpnekraft maks 30N", d.ApnekraftMaks30N));

                        foreach (var type in new[] { "Daglås", "Nattlås" })
                        {
                            var l = d.Lassystemer.FirstOrDefault(x => x.Type == type);
                            if (l is null)
                            {
                                continue;
                            }

                            Seksjon(col, type,
                                ("Låskasse", l.Laskasse), ("Mek. sluttstykke", l.MekSluttstykke), ("Mikrobryter", l.Mikrobryter),
                                ("El. sluttstykke", l.ElSluttstykke), ("Stolpe", l.Stolpe), ("Volt", l.Volt),
                                ("Karmoverføring", l.Karmoverforing), ("Festelepper", l.Festelepper), ("Kabel", l.Kabel),
                                ("Dørvrider", l.Dorvrider), ("Skilt", l.Skilt), ("Overflate", l.Overflate),
                                ("Sylinder", l.Sylinder), ("Dørtykkelse A/B", l.DortykkelseAB), ("Magnetkontakt", l.Magnetkontakt),
                                ("Nødutstyr", l.Nodutstyr), ("Annet utstyr", l.AnnetUtstyr));
                        }

                        Seksjon(col, "Dørlukker",
                            ("Dørlukker", d.Dorlukker), ("Arm/glideskinne", d.ArmGlideskinne), ("VK/plate", d.VkPlate),
                            ("Montasje side", d.MontasjeSideDorlukker), ("Annet utstyr", d.AnnetUtstyrDorlukker));

                        Seksjon(col, "Automatikk",
                            ("Automatikk", d.Automatikk), ("Trekk/skyv.arm", d.TrekkSkyvArm), ("Adapter", d.Adapter),
                            ("Montasje side", d.MontasjeSideAutomatikk), ("Albuekontakter", d.Albuekontakter),
                            ("Radar/sensor", d.RadarSensor), ("Kabel", d.KabelAutomatikk), ("UPS/nødstrøm", d.UpsNodstrom),
                            ("Sikkerhetssensor", d.Sikkerhetssensor));

                        Seksjon(col, "Øvrig beslag",
                            ("Magnetlås", d.Magnetlas), ("Brakett til ML", d.BrakettMl), ("Panikkbeslag/skåte", d.Panikkbeslag),
                            ("Håndtak", d.Handtak), ("Annet utstyr", d.AnnetUtstyrOvrig));

                        if (!string.IsNullOrWhiteSpace(d.Notater))
                        {
                            col.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("Anm./notater: ").SemiBold();
                                t.Span(d.Notater);
                            });
                        }

                        if (d.Bilder.Count > 0)
                        {
                            foreach (var chunk in d.Bilder.Chunk(4))
                            {
                                col.Item().PaddingTop(6).Row(row =>
                                {
                                    foreach (var bilde in chunk)
                                    {
                                        row.RelativeItem().Padding(2).Height(90).Image(bilde.Data).FitArea();
                                    }
                                });
                            }
                        }
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Column(c =>
                {
                    c.Item().AlignCenter().Text($"{FirmaNavn} - {FirmaAdresse} - Tlf {FirmaTelefon} - {FirmaEpost}").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Seksjon(ColumnDescriptor col, string tittel, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().PaddingTop(6).Text(tittel).FontSize(9).SemiBold().FontColor(Colors.Grey.Darken2);

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });

            foreach (var (label, verdi) in utfylte)
            {
                table.Cell().Padding(2).Text(t =>
                {
                    t.Span($"{label}: ").SemiBold();
                    t.Span(verdi);
                });
            }

            if (utfylte.Count % 2 == 1)
            {
                table.Cell();
            }
        });
    }
}
