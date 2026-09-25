using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class BefaringPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private static readonly Color Sand = Color.FromHex("#F2EBE1");
    private static readonly Color SandBorder = Color.FromHex("#E4D9C8");
    private static readonly Color Accent = Color.FromHex("#835E41");
    private static readonly Color Ink = Color.FromHex("#292927");

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
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BEFARINGSRAPPORT").FontSize(8).Bold().FontColor(Accent).LetterSpacing(0.08f);
                            c.Item().PaddingTop(2).Text(befaring.Navn).FontSize(19).Bold();
                            c.Item().PaddingTop(3).Row(meta =>
                            {
                                meta.AutoItem().Text($"Dato {befaring.Dato?.ToString("dd.MM.yyyy") ?? "-"}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                meta.AutoItem().PaddingLeft(12).Text($"Befart av {befaring.BefartAv ?? "-"}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(befaring.SystemNr))
                                {
                                    meta.AutoItem().PaddingLeft(12).Text($"System nr {befaring.SystemNr}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });
                        row.ConstantItem(120).AlignRight().Element(e => pdfLogo.Render(e, 26));
                    });
                    col.Item().PaddingTop(8).BorderBottom(2.5f).BorderColor(Accent);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(10);

                    KontaktBoks(col,
                        ("Kunde", befaring.Kundenavn), ("Bygg/prosjekt", befaring.Bygg),
                        ("Adresse", befaring.Adresse), ("Post nr / sted", befaring.Sted),
                        ("Kontaktperson", befaring.Kontaktperson), ("Tlf/mobil", befaring.Tlf),
                        ("E-post", befaring.Epost));

                    if (!string.IsNullOrWhiteSpace(befaring.Oppdrag))
                    {
                        col.Item().Text(t =>
                        {
                            t.Span("Oppdrag: ").SemiBold();
                            t.Span(befaring.Oppdrag);
                        });
                    }

                    foreach (var d in befaring.Dorfelt)
                    {
                        var tittel = string.IsNullOrWhiteSpace(d.Dornavn) ? "Dørfelt" : d.Dornavn;
                        col.Item().Column(dc => DorfeltBlokk(dc, d, tittel));
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(SandBorder).PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text($"{FirmaInfo.Navn} - {FirmaInfo.AdresseFull} - Tlf {FirmaInfo.Telefon}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        t.Span(" / ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        t.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void KontaktBoks(ColumnDescriptor col, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().Background(Sand).Border(1).BorderColor(SandBorder).Padding(10).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            foreach (var (label, verdi) in utfylte)
            {
                table.Cell().Padding(3).Column(c =>
                {
                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).Bold().FontColor(Accent).LetterSpacing(0.03f);
                    c.Item().PaddingTop(1).Text(verdi).FontSize(9.3f).SemiBold();
                });
            }
        });
    }

    private static void DorfeltBlokk(ColumnDescriptor col, Models.BefaringDorfelt d, string tittel)
    {
        col.Item().Background(Accent).Padding(7).Row(row =>
        {
            row.RelativeItem().Text(tittel).FontSize(11).Bold().FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(d.Dornr))
            {
                row.AutoItem().Background(Color.FromHex("#A5805F")).PaddingVertical(2).PaddingHorizontal(8).Text($"Dør {d.Dornr}").FontSize(8).Bold().FontColor(Colors.White);
            }
        });

        col.Item().Border(1).BorderColor(SandBorder).Padding(10).Column(inner =>
        {
            inner.Spacing(6);

            FeltSeksjon(inner, "Identifikasjon",
                ("Dørtype", d.Dortype), ("Fløyer", d.Floyer), ("BxH", d.BxH),
                ("Slagretning", d.Slagretning), ("Låssystemnr", d.Lassystemnr));

            KravPiller(inner,
                ("Brannklasse", d.Brannklasse), ("FG/Beskyttelsesklasse", d.Fg), ("Sikringsklasse", d.Sikringsklasse),
                ("Risikoklasse", d.Risikoklasse), ("Universell utforming", d.UniversellUtforming),
                ("Åpnekraft maks 30N", d.ApnekraftMaks30N), ("Brannkrav", d.Brannkrav));

            foreach (var type in new[] { "Daglås", "Nattlås" })
            {
                var l = d.Lassystemer.FirstOrDefault(x => x.Type == type);
                if (l is null)
                {
                    continue;
                }

                FeltSeksjon(inner, type,
                    ("Låskasse", l.Laskasse), ("Mek. sluttstykke", l.MekSluttstykke), ("Mikrobryter", l.Mikrobryter),
                    ("El. sluttstykke", l.ElSluttstykke), ("Stolpe", l.Stolpe), ("Volt", l.Volt),
                    ("Karmoverføring", l.Karmoverforing), ("Festelepper", l.Festelepper), ("Kabel", l.Kabel),
                    ("Dørvrider", l.Dorvrider), ("Skilt", l.Skilt), ("Overflate", l.Overflate),
                    ("Sylinder", l.Sylinder), ("Dørtykkelse A/B", l.DortykkelseAB), ("Magnetkontakt", l.Magnetkontakt),
                    ("Nødutstyr", l.Nodutstyr), ("Annet utstyr", l.AnnetUtstyr));
            }

            FeltSeksjon(inner, "Dørlukker",
                ("Dørlukker", d.Dorlukker), ("Arm/glideskinne", d.ArmGlideskinne), ("VK/plate", d.VkPlate),
                ("Montasje side", d.MontasjeSideDorlukker), ("Annet utstyr", d.AnnetUtstyrDorlukker));

            FeltSeksjon(inner, "Automatikk",
                ("Automatikk", d.Automatikk), ("Trekk/skyv.arm", d.TrekkSkyvArm), ("Adapter", d.Adapter),
                ("Montasje side", d.MontasjeSideAutomatikk), ("Albuekontakter", d.Albuekontakter),
                ("Radar/sensor", d.RadarSensor), ("Kabel", d.KabelAutomatikk), ("UPS/nødstrøm", d.UpsNodstrom),
                ("Sikkerhetssensor", d.Sikkerhetssensor));

            FeltSeksjon(inner, "Øvrig beslag",
                ("Magnetlås", d.Magnetlas), ("Brakett til ML", d.BrakettMl), ("Panikkbeslag/skåte", d.Panikkbeslag),
                ("Håndtak", d.Handtak), ("Annet utstyr", d.AnnetUtstyrOvrig));

            if (!string.IsNullOrWhiteSpace(d.Notater))
            {
                inner.Item().Background(Sand).BorderLeft(2.5f).BorderColor(Accent).Padding(7).Text(t =>
                {
                    t.Span("Anm./notater: ").SemiBold();
                    t.Span(d.Notater);
                });
            }

            if (d.Bilder.Count > 0)
            {
                foreach (var chunk in d.Bilder.Chunk(4))
                {
                    inner.Item().Row(row =>
                    {
                        foreach (var bilde in chunk)
                        {
                            row.RelativeItem().Padding(2).Border(1).BorderColor(SandBorder).Height(90).Image(bilde.Data).FitArea();
                        }
                    });
                }
            }
        });
    }

    private static void FeltSeksjon(ColumnDescriptor col, string tittel, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().Text(tittel.ToUpperInvariant()).FontSize(7.2f).Bold().FontColor(Accent).LetterSpacing(0.03f);
        col.Item().PaddingBottom(1).BorderBottom(0.75f).BorderColor(SandBorder);

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            foreach (var (label, verdi) in utfylte)
            {
                table.Cell().Padding(2).Text(t =>
                {
                    t.Span($"{label}: ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(verdi).FontSize(8.5f).SemiBold();
                });
            }
        });
    }

    private static void KravPiller(ColumnDescriptor col, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().Text("KRAV").FontSize(7.2f).Bold().FontColor(Accent).LetterSpacing(0.03f);
        col.Item().PaddingBottom(1).BorderBottom(0.75f).BorderColor(SandBorder);

        col.Item().Row(row =>
        {
            row.Spacing(4);
            foreach (var (label, verdi) in utfylte)
            {
                row.AutoItem().Background(Accent).PaddingVertical(3).PaddingHorizontal(7).Text($"{label}: {verdi}").FontSize(7.5f).Bold().FontColor(Colors.White);
            }
        });
    }
}
