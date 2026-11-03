using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class AvvikPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");


    public async Task<byte[]?> GenerateAsync(int avvikId)
    {
        var avvik = await db.Avvik
            .Include(a => a.Dor).ThenInclude(d => d!.Prosjekt)
            .Include(a => a.AnsvarligBruker)
            .Include(a => a.Media)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == avvikId);

        if (avvik is null)
        {
            return null;
        }

        var dor = avvik.Dor;
        var prosjektNavn = dor?.Prosjekt?.Navn ?? "";
        var bilder = avvik.Media.Where(m => m.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Id).ToList();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Avviksmelding", $"{prosjektNavn} · Dør {dor?.Dornummer}"));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 3,
                        ("Avvik", avvik.NummerTekst), ("Prosjekt", prosjektNavn), ("Dør", dor?.Dornummer),
                        ("Dato", avvik.OpprettetDato.ToString("dd.MM.yyyy")), ("Prioritet", avvik.Prioritet.Visningsnavn()),
                        ("Ansvarlig", avvik.AnsvarligBruker?.Navn ?? "-"));

                    if (!string.IsNullOrWhiteSpace(avvik.Tittel))
                    {
                        col.Item().Text(avvik.Tittel).FontSize(13).SemiBold();
                    }

                    PdfStil.FriSeksjon(col, "Beskrivelse av avvik", avvik.Beskrivelse);

                    // Bildene to og to, rotert slik de er satt på avvikssiden.
                    if (bilder.Count > 0)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Bilder");
                            foreach (var par in bilder.Chunk(2))
                            {
                                inner.Item().PaddingTop(6).Row(row =>
                                {
                                    row.Spacing(8);
                                    foreach (var b in par)
                                    {
                                        var celle = row.RelativeItem().Height(190).Border(1).BorderColor(PdfStil.SandBorder).Padding(3).AlignCenter().AlignMiddle();
                                        var rotert = b.Rotasjon switch
                                        {
                                            90 => celle.RotateRight(),
                                            180 => celle.RotateRight().RotateRight(),
                                            270 => celle.RotateLeft(),
                                            _ => celle
                                        };
                                        rotert.Image(b.Data).FitArea();
                                    }

                                    if (par.Length == 1)
                                    {
                                        row.RelativeItem();
                                    }
                                });
                            }
                        });
                    }

                    PdfStil.FriSeksjon(col, "Foreslått utbedring", avvik.UtbedringBeskrivelse);

                    if (avvik.Pris.HasValue)
                    {
                        col.Item().Column(inner =>
                        {
                            PdfStil.SeksjonTittel(inner, "Pris for utbedring");
                            inner.Item().PaddingTop(3).Text($"{avvik.Pris.Value.ToString("N2", Kultur)} kr eks. mva").FontSize(9);
                        });
                    }

                    col.Item().Column(inner =>
                    {
                        PdfStil.SeksjonTittel(inner, "Godkjenning");
                        if (avvik.Signatur is not null)
                        {
                            inner.Item().PaddingTop(4).Background(PdfStil.Sand).Border(1).BorderColor(PdfStil.SandBorder).Padding(6)
                                .Height(70).Width(200).Image(avvik.Signatur).FitArea();
                            inner.Item().PaddingTop(3).Text($"{avvik.SignertAvNavn} - signert {avvik.SignertDato?.ToString("dd.MM.yyyy HH:mm")}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            inner.Item().PaddingTop(4).Background(PdfStil.Sand).Border(1).BorderColor(PdfStil.SandBorder).Padding(6)
                                .Height(60).AlignMiddle().AlignCenter().Text("Signatur / navn / dato").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                        }
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }

    // Alle avvik på et prosjekt i én PDF: oversikt øverst, deretter hvert avvik med
    // beskrivelse, bilder (inntil fire), foreslått utbedring og status.
    public async Task<byte[]?> GenerateForProsjektAsync(int prosjektId)
    {
        var prosjekt = await db.Prosjekter.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var avvik = await db.Avvik.AsNoTracking()
            .Where(a => a.Dor != null && a.Dor.ProsjektId == prosjektId)
            .Include(a => a.Dor)
            .Include(a => a.AnsvarligBruker)
            .Include(a => a.OpprettetAvBruker)
            .Include(a => a.Media)
            .AsSplitQuery()
            .OrderBy(a => a.Status == AvvikStatus.Lukket).ThenBy(a => a.OpprettetDato)
            .ToListAsync();

        var hodeBak = Color.FromHex("#E9ECF0");

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Avviksrapport", prosjekt.Navn));

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);

                    PdfStil.InfoBoks(col, 4,
                        ("Prosjekt", prosjekt.Navn), ("Avvik", avvik.Count.ToString()),
                        ("Åpne", avvik.Count(a => a.Status != AvvikStatus.Lukket).ToString()),
                        ("Dato", DateTime.Today.ToString("dd.MM.yyyy")));

                    if (avvik.Count == 0)
                    {
                        col.Item().Text("Ingen avvik er meldt på prosjektet.").FontSize(10);
                        return;
                    }

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(62);
                            c.RelativeColumn(3);
                            c.ConstantColumn(50);
                            c.ConstantColumn(52);
                            c.RelativeColumn(1.3f);
                            c.ConstantColumn(62);
                        });
                        t.Header(h =>
                        {
                            foreach (var tittel in new[] { "Nr.", "Avvik", "Dør", "Prioritet", "Status", "Meldt" })
                            {
                                h.Cell().Background(hodeBak).PaddingVertical(5).PaddingHorizontal(6).Text(tittel).FontSize(8).Bold();
                            }
                        });
                        var nr = 0;
                        foreach (var a in avvik)
                        {
                            var alt = nr++ % 2 == 1;
                            PdfStil.TabellRad(t.Cell(), alt).Text(a.NummerTekst).SemiBold();
                            PdfStil.TabellRad(t.Cell(), alt).Text(a.Overskrift);
                            PdfStil.TabellRad(t.Cell(), alt).Text(a.Dor?.Dornummer ?? "-");
                            PdfStil.TabellRad(t.Cell(), alt).Text(a.Prioritet.Visningsnavn());
                            PdfStil.TabellRad(t.Cell(), alt).Text(a.Status.Visningsnavn());
                            PdfStil.TabellRad(t.Cell(), alt).Text(a.OpprettetDato.ToString("dd.MM.yyyy"));
                        }
                    });

                    foreach (var a in avvik)
                    {
                        col.Item().PaddingTop(8).PreventPageBreak().Border(0.75f).BorderColor(PdfStil.SandBorder).Column(k =>
                        {
                            k.Item().Background(hodeBak).PaddingVertical(6).PaddingHorizontal(9).Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span(a.NummerTekst + "  ").Bold();
                                    t.Span(a.Overskrift).SemiBold();
                                });
                                r.AutoItem().Text($"{a.Status.Visningsnavn()} · {a.Prioritet.Visningsnavn()}").FontSize(8.5f);
                            });
                            k.Item().Padding(9).Column(inner =>
                            {
                                inner.Spacing(6);
                                inner.Item().Text($"Dør {a.Dor?.Dornummer} · meldt {a.OpprettetDato:dd.MM.yyyy} av {a.OpprettetAvBruker?.Navn ?? "ukjent"}"
                                    + (a.AnsvarligBruker is null ? "" : $" · ansvarlig {a.AnsvarligBruker.Navn}")
                                    + (a.LukketDato is { } ld ? $" · lukket {ld:dd.MM.yyyy}" : "")).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                PdfStil.FriSeksjon(inner, "Beskrivelse av avvik", a.Beskrivelse);
                                var bilder = a.Media.Where(m => m.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Id).Take(4).ToList();
                                foreach (var par in bilder.Chunk(2))
                                {
                                    inner.Item().Row(row =>
                                    {
                                        row.Spacing(8);
                                        foreach (var b in par)
                                        {
                                            var celle = row.RelativeItem().Height(150).Border(1).BorderColor(PdfStil.SandBorder).Padding(3).AlignCenter().AlignMiddle();
                                            var rotert = b.Rotasjon switch
                                            {
                                                90 => celle.RotateRight(),
                                                180 => celle.RotateRight().RotateRight(),
                                                270 => celle.RotateLeft(),
                                                _ => celle
                                            };
                                            rotert.Image(b.Data).FitArea();
                                        }

                                        if (par.Length == 1)
                                        {
                                            row.RelativeItem();
                                        }
                                    });
                                }
                                if (!string.IsNullOrWhiteSpace(a.UtbedringBeskrivelse))
                                {
                                    PdfStil.FriSeksjon(inner, "Foreslått utbedring", a.UtbedringBeskrivelse);
                                }
                            });
                        });
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });
        });

        return document.GeneratePdf();
    }
}
