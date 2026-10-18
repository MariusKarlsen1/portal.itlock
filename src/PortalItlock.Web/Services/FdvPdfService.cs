using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace PortalItlock.Web.Services;

public class FdvPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<List<Component>> HentKomponenterMedFdvAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var komponentIder = await db.DorKomponenter
            .Where(dk => dk.Dor!.ProsjektId == prosjektId
                && (valgteByggetrinn == null || (dk.Dor!.Plantegning != null && valgteByggetrinn.Contains(dk.Dor!.Plantegning!.Byggetrinn))))
            .Select(dk => dk.ComponentId)
            .Distinct()
            .ToListAsync();

        // Filene hentes for seg på Id - c.Filer i en join/Include leser alle
        // filene i databasen (se KomponentFilerExtensions).
        var medFdv = await db.KomponenterMedFilAsync("FdvData", komponentIder);
        var ider = komponentIder.Where(medFdv.Contains).ToList();
        var komponenter = await db.Components
            .Where(c => ider.Contains(c.Id))
            .Include(c => c.FdvDokumenter)
            .OrderBy(c => c.Navn)
            .ToListAsync();
        await db.Set<ComponentFiler>().Where(f => ider.Contains(f.Id)).LoadAsync();
        return komponenter;
    }

    // Mal F7 (fdv-pdf-forslag.html): forside med fargekolonne (kontaktperson,
    // service og feil, firma), innholdsfortegnelse med sidetall, og FDV-bladene
    // fra produsentene med en tynn topplinje (produkt, produsent og kode) og
    // sidetall nederst. Avsenderen er organisasjonen som er logget inn.
    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);
        var komponenter = await HentKomponenterMedFdvAsync(prosjektId, byggetrinn);
        var ekstraVedlegg = await db.FdvVedlegg
            .Where(v => v.ProsjektId == prosjektId)
            .OrderBy(v => v.Navn)
            .ToListAsync();

        if (komponenter.Count == 0 && ekstraVedlegg.Count == 0)
        {
            return null;
        }

        var prosjekt = await db.Prosjekter
            .Include(p => p.Kunde)
            .Include(p => p.Prosjektleder)
            .FirstOrDefaultAsync(p => p.Id == prosjektId);

        var org = RapportProfil.Fra(tenantContext.Current);
        var prosjektNavn = prosjekt?.Navn ?? "Prosjekt";
        var kontaktNavn = prosjekt?.Prosjektleder?.Navn ?? (org.ErStandard ? FirmaInfo.Kontaktperson : null);
        var kontaktEpost = prosjekt?.Prosjektleder?.Epost ?? org.Epost;
        var kunde = prosjekt?.Kunde;
        var kundeAdresse = kunde is null ? null : string.Join(", ", new[] { kunde.Adresse, string.Join(" ", new[] { kunde.Postnr, kunde.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var bunnLinje = string.Join(" · ", new[] { org.Navn, org.Adresse, org.OrgNr is null ? null : $"Org.nr {org.OrgNr}" }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var harInnledning = !string.IsNullOrWhiteSpace(prosjekt?.FdvForside);

        // Alle dokumentene som skal inn, i rekkefølge - hvert med egen seksjon
        // slik at innholdsfortegnelsen kan vise sidetallet det starter på.
        var deler = new List<(string Seksjon, string Tittel, string? Undertittel, byte[] Data)>();
        var nr = 0;
        foreach (var komponent in komponenter)
        {
            var undertittel = string.Join(" · ", new[] { komponent.Produsent, komponent.Produktkode }.Where(s => !string.IsNullOrWhiteSpace(s)));
            deler.Add(($"fdv-{nr++}", komponent.Navn, undertittel, komponent.FdvData!));
            foreach (var ekstraDokument in komponent.FdvDokumenter)
            {
                deler.Add(($"fdv-{nr++}", $"{komponent.Navn} – {ekstraDokument.Filnavn}", undertittel, ekstraDokument.Data));
            }
        }
        var forsteVedlegg = nr;
        foreach (var vedlegg in ekstraVedlegg)
        {
            deler.Add(($"fdv-{nr++}", vedlegg.Navn, "Vedlegg", vedlegg.Data));
        }

        // Produsentens PDF-sider gjøres om til bilder på forhånd. Dokumenter som
        // ikke kan leses hoppes over (og vises ikke i innholdet).
        var sider = new Dictionary<string, List<byte[]>>();
        foreach (var del in deler)
        {
            try
            {
                var bilder = new List<byte[]>();
                foreach (var bitmap in PDFtoImage.Conversion.ToImages(del.Data, options: new PDFtoImage.RenderOptions(Dpi: 150)))
                {
                    using (bitmap)
                    using (var image = SKImage.FromBitmap(bitmap))
                    using (var encoded = image.Encode(SKEncodedImageFormat.Png, 85))
                    {
                        bilder.Add(encoded.ToArray());
                    }
                }
                if (bilder.Count > 0)
                {
                    sider[del.Seksjon] = bilder;
                }
            }
            catch (Exception)
            {
                // Ugyldig eller skadet PDF - hoppes over.
            }
        }
        deler = deler.Where(d => sider.ContainsKey(d.Seksjon)).ToList();

        static void Etikett(ColumnDescriptor c, string tekst, Color farge) =>
            c.Item().Text(tekst.ToUpperInvariant()).FontSize(7f).Bold().FontColor(farge).LetterSpacing(0.08f);

        void Bunn(PageDescriptor page) => page.Footer().BorderTop(0.5f).BorderColor(PdfStil.SandBorder).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(bunnLinje).FontSize(7).FontColor(Colors.Grey.Darken1);
            row.AutoItem().Text(x =>
            {
                x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                x.Span($"FDV · {prosjektNavn} · side ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });

        void Hode(PageDescriptor page) => page.Header().Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().AlignBottom().Element(e => org.TegnLogo(e, pdfLogo, 20));
                row.RelativeItem().AlignRight().AlignBottom().Column(c =>
                {
                    c.Item().AlignRight().Text("FDV-dokumentasjon").FontSize(9).SemiBold();
                    c.Item().AlignRight().Text(prosjekt?.OverlevertDato is { } od ? $"{prosjektNavn} · overlevert {od:dd.MM.yyyy}" : prosjektNavn).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingTop(6).BorderBottom(2).BorderColor(org.Aksent);
        });

        var document = Document.Create(doc =>
        {
            // Forside
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(PdfStil.Ink));

                page.Content().Row(row =>
                {
                    row.ConstantItem(160).Background(org.AksentLys).PaddingVertical(42).PaddingHorizontal(20).Column(kol =>
                    {
                        kol.Item().Element(e => org.TegnLogo(e, pdfLogo, 26));
                        kol.Item().ExtendVertical().AlignBottom().Column(bunn =>
                        {
                            if (!string.IsNullOrWhiteSpace(kontaktNavn))
                            {
                                Etikett(bunn, "Kontaktperson", org.Aksent);
                                bunn.Item().PaddingTop(2).Text(kontaktNavn).FontSize(9.5f).SemiBold();
                                if (!string.IsNullOrWhiteSpace(kontaktEpost))
                                {
                                    bunn.Item().Text(kontaktEpost).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                                }
                                bunn.Item().Height(12);
                            }
                            if (!string.IsNullOrWhiteSpace(org.Telefon) || !string.IsNullOrWhiteSpace(org.Epost))
                            {
                                Etikett(bunn, "Service og feil", org.Aksent);
                                if (!string.IsNullOrWhiteSpace(org.Telefon))
                                {
                                    bunn.Item().PaddingTop(2).Text(org.Telefon).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                                }
                                if (!string.IsNullOrWhiteSpace(org.Epost))
                                {
                                    bunn.Item().Text(org.Epost).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                                }
                                bunn.Item().Height(12);
                            }
                            Etikett(bunn, "Firma", org.Aksent);
                            bunn.Item().PaddingTop(2).Text(org.Navn).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                            if (!string.IsNullOrWhiteSpace(org.Adresse))
                            {
                                bunn.Item().Text(org.Adresse).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                            }
                            if (!string.IsNullOrWhiteSpace(org.OrgNr))
                            {
                                bunn.Item().Text($"Org.nr {org.OrgNr}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                            }
                        });
                    });

                    row.RelativeItem().PaddingVertical(42).PaddingHorizontal(40).Column(kol =>
                    {
                        kol.Item().Height(150);
                        Etikett(kol, "FDV-dokumentasjon", org.Aksent);
                        kol.Item().PaddingTop(6).Text(prosjektNavn).FontSize(28).Bold().LineHeight(1.15f);
                        kol.Item().PaddingTop(4).Text("Forvaltning, drift og vedlikehold").FontSize(12).FontColor(Colors.Grey.Darken2);

                        if (kunde is not null)
                        {
                            kol.Item().PaddingTop(56).Column(eier =>
                            {
                                Etikett(eier, "Byggeier", org.Aksent);
                                eier.Item().PaddingTop(2).Text(kunde.Navn).FontSize(12).SemiBold();
                                if (!string.IsNullOrWhiteSpace(kundeAdresse))
                                {
                                    eier.Item().Text(kundeAdresse).FontSize(9).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        }

                        kol.Item().PaddingTop(22).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            void Felt(string etikett, string verdi) => t.Cell().PaddingBottom(10).Column(c =>
                            {
                                Etikett(c, etikett, org.Aksent);
                                c.Item().PaddingTop(1).Text(verdi).FontSize(10).SemiBold();
                            });

                            Felt("Overlevert", prosjekt?.OverlevertDato?.ToString("dd.MM.yyyy") ?? "-");
                            Felt("System", string.IsNullOrWhiteSpace(prosjekt?.System) ? "-" : prosjekt!.System!);
                            Felt("Produkter", $"{komponenter.Count} med FDV");
                            Felt("Byggetrinn", valgteByggetrinn is null ? "Alle" : string.Join(", ", valgteByggetrinn));
                        });
                    });
                });
            });

            // Innhold
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(PdfStil.Ink));
                Hode(page);

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Text("Innhold").FontSize(16).Bold();
                    col.Item().Height(8);

                    void Linje(string tekst, string seksjon, bool del, string? under = null) => col.Item()
                        .BorderBottom(0.5f).BorderColor(PdfStil.SandBorder).PaddingVertical(del ? 5 : 3.5f).PaddingLeft(del ? 0 : 14).Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                var span = t.Span(tekst);
                                if (del)
                                {
                                    span.Bold().FontColor(org.Aksent);
                                }
                                if (!string.IsNullOrWhiteSpace(under))
                                {
                                    t.Span($"  ·  {under}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                }
                            });
                            r.AutoItem().Text(t =>
                            {
                                var s = t.BeginPageNumberOfSection(seksjon);
                                if (del)
                                {
                                    s.Bold();
                                }
                            });
                        });

                    var delNr = 1;
                    if (harInnledning)
                    {
                        Linje($"{delNr++}  Innledning", "innledning", true);
                    }

                    var produkter = deler.Where(d => int.Parse(d.Seksjon[4..]) < forsteVedlegg).ToList();
                    if (produkter.Count > 0)
                    {
                        Linje($"{delNr++}  Produktdokumentasjon", produkter[0].Seksjon, true);
                        foreach (var d in produkter)
                        {
                            Linje(d.Tittel, d.Seksjon, false, d.Undertittel);
                        }
                    }

                    var vedleggDeler = deler.Where(d => int.Parse(d.Seksjon[4..]) >= forsteVedlegg).ToList();
                    if (vedleggDeler.Count > 0)
                    {
                        Linje($"{delNr}  Vedlegg", vedleggDeler[0].Seksjon, true);
                        foreach (var d in vedleggDeler)
                        {
                            Linje(d.Tittel, d.Seksjon, false);
                        }
                    }
                });

                Bunn(page);
            });

            // Innledning (prosjektets FDV-forside-tekst)
            if (harInnledning)
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.8f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(PdfStil.Ink));
                    Hode(page);
                    page.Content().PaddingTop(14).Section("innledning").Column(col => ForsideRenderer.Render(col, prosjekt!.FdvForside!));
                    Bunn(page);
                });
            }

            // FDV-bladene: tynn topplinje med produkt, produsent og kode
            foreach (var del in deler)
            {
                var bilder = sider[del.Seksjon];
                for (var i = 0; i < bilder.Count; i++)
                {
                    var bildeData = bilder[i];
                    var erForste = i == 0;
                    doc.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                        page.Header().BorderBottom(1.2f).BorderColor(org.Aksent).PaddingBottom(3).Row(row =>
                        {
                            row.RelativeItem().Text(del.Tittel).FontSize(9).Bold();
                            if (!string.IsNullOrWhiteSpace(del.Undertittel))
                            {
                                row.AutoItem().AlignBottom().Text(del.Undertittel).FontSize(8).FontColor(Colors.Grey.Darken1);
                            }
                        });

                        var innhold = page.Content().PaddingVertical(6);
                        if (erForste)
                        {
                            innhold = innhold.Section(del.Seksjon);
                        }
                        innhold.AlignCenter().AlignMiddle().Image(bildeData).FitArea();

                        Bunn(page);
                    });
                }
            }
        });

        return document.GeneratePdf();
    }
}
