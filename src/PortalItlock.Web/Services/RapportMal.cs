using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Felles byggesteiner for rapportmalen "Klassisk" (oktober 2026), brukt av
// arbeidsrapporten og servicerapporten slik at de ser like ut: topp med logo,
// tittel, rapportnummer og status, nøkkelinfo i en rolig boks, seksjoner med
// små overskrifter, anbefalinger i en markert boks, sjekkliste, bilder,
// signaturlinjer og bunntekst. Logo, navn, kontaktinfo og farge hentes fra
// organisasjonen som er logget inn (se RapportProfil). Priser og timer hører
// ikke hjemme i disse rapportene.
public static class RapportMal
{
    public static readonly Color Dempet = Color.FromHex("#6D675F");
    public static readonly Color Svak = Color.FromHex("#8B857D");
    public static readonly Color Linje = Color.FromHex("#E6DFD4");
    private static readonly Color Gronn = Color.FromHex("#2E7D4F");
    private static readonly Color GronnLys = Color.FromHex("#E3F1E8");
    private static readonly Color Gul = Color.FromHex("#C9902F");
    private static readonly Color GulLys = Color.FromHex("#FDF6EA");

    public static void Side(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(1.8f, Unit.Centimetre);
        page.MarginTop(1.6f, Unit.Centimetre);
        page.MarginBottom(1.4f, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(PdfStil.Ink).LineHeight(1.35f));
    }

    public static void Topp(ColumnDescriptor col, RapportProfil org, PdfLogo pdfLogo, string tittel, string? undertittel,
        string nummer, DateTime dato, bool erUtfort, string statusTekst)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(v =>
            {
                v.Item().Element(e => org.TegnLogo(e, pdfLogo));
                v.Item().PaddingTop(14).Text(tittel).FontSize(20).Bold().FontColor(PdfStil.Ink);
                if (!string.IsNullOrWhiteSpace(undertittel))
                {
                    v.Item().PaddingTop(1).Text(undertittel).FontSize(10).FontColor(Dempet);
                }
            });
            row.ConstantItem(130).AlignRight().Column(h =>
            {
                h.Item().AlignRight().Text("RAPPORT").FontSize(7).Bold().FontColor(Svak).LetterSpacing(0.06f);
                h.Item().AlignRight().Text(nummer).FontSize(13).Bold();
                h.Item().AlignRight().Text(dato.ToString("dd.MM.yyyy")).FontSize(9).FontColor(Dempet);
                h.Item().PaddingTop(5).AlignRight()
                    .Background(erUtfort ? GronnLys : org.AksentLys).PaddingVertical(2).PaddingHorizontal(8)
                    .Text(erUtfort ? "✓ Utført" : statusTekst).FontSize(8).Bold().FontColor(erUtfort ? Gronn : org.Aksent);
            });
        });
        col.Item().PaddingTop(10).LineHorizontal(2.5f).LineColor(org.Aksent);
    }

    public static void InfoBoks(ColumnDescriptor col, RapportProfil org, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().Background(org.AksentLys).Border(0.75f).BorderColor(Linje).Padding(11).Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
            foreach (var (label, verdi) in utfylte)
            {
                table.Cell().PaddingVertical(4).PaddingRight(10).Column(c =>
                {
                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).SemiBold().FontColor(Svak).LetterSpacing(0.05f);
                    c.Item().PaddingTop(1).Text(verdi).FontSize(9.5f).SemiBold();
                });
            }
        });
    }

    public static void Overskrift(ColumnDescriptor col, string tittel, RapportProfil org) =>
        col.Item().Text(tittel.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(org.Aksent).LetterSpacing(0.09f);

    public static void Seksjon(ColumnDescriptor col, string tittel, RapportProfil org, string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return;
        }

        col.Item().Column(c =>
        {
            Overskrift(c, tittel, org);
            c.Item().PaddingTop(4).Text(tekst);
        });
    }

    public static void Anbefalinger(ColumnDescriptor col, RapportProfil org, string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return;
        }

        col.Item().Column(c =>
        {
            Overskrift(c, "Anbefalinger", org);
            c.Item().PaddingTop(5).Background(GulLys).BorderLeft(3).BorderColor(Gul)
                .PaddingVertical(8).PaddingHorizontal(11).Text(tekst);
        });
    }

    public static void Sjekkliste(ColumnDescriptor col, RapportProfil org, IEnumerable<(bool Fullfort, string Tekst)> punkter)
    {
        var liste = punkter.ToList();
        if (liste.Count == 0)
        {
            return;
        }

        col.Item().Column(c =>
        {
            Overskrift(c, "Sjekkliste", org);
            c.Item().PaddingTop(5).Column(l =>
            {
                l.Spacing(4);
                foreach (var (fullfort, tekst) in liste)
                {
                    l.Item().Row(r =>
                    {
                        r.ConstantItem(17).PaddingTop(1).Element(e =>
                        {
                            var boks = e.Width(11).Height(11);
                            if (fullfort)
                            {
                                boks.Background(org.Aksent).AlignMiddle().AlignCenter().Text("✓").FontSize(7.5f).Bold().FontColor(Colors.White);
                            }
                            else
                            {
                                boks.Border(1.2f).BorderColor(org.Aksent);
                            }
                        });
                        r.RelativeItem().Text(tekst).FontColor(fullfort ? PdfStil.Ink : Dempet);
                    });
                }
            });
        });
    }

    // Enkel tabell med lette linjer: overskrift i små versaler, siste kolonne
    // høyrejustert når den er et antall. Bredder: positivt tall er fast bredde
    // i punkter, negativt tall er relativ andel (-2 = dobbelt så bred som -1).
    public static void Tabell(ColumnDescriptor col, string tittel, RapportProfil org, string[] kolonner, float[] bredder,
        IEnumerable<string[]> rader, bool sisteHoyre)
    {
        var liste = rader.ToList();
        if (liste.Count == 0)
        {
            return;
        }

        col.Item().Column(c =>
        {
            Overskrift(c, tittel, org);
            c.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    foreach (var b in bredder)
                    {
                        if (b > 0) { cd.ConstantColumn(b); } else { cd.RelativeColumn(-b); }
                    }
                });
                table.Header(h =>
                {
                    for (var i = 0; i < kolonner.Length; i++)
                    {
                        var celle = h.Cell().BorderBottom(1.2f).BorderColor(Linje).PaddingVertical(4).PaddingRight(6);
                        (sisteHoyre && i == kolonner.Length - 1 ? celle.AlignRight() : celle)
                            .Text(kolonner[i].ToUpperInvariant()).FontSize(7).SemiBold().FontColor(Svak);
                    }
                });
                foreach (var rad in liste)
                {
                    for (var i = 0; i < rad.Length; i++)
                    {
                        var celle = table.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingRight(6);
                        (sisteHoyre && i == rad.Length - 1 ? celle.AlignRight() : celle).Text(rad[i]);
                    }
                }
            });
        });
    }

    public static void Bilder(ColumnDescriptor col, RapportProfil org, IEnumerable<byte[]> bilder)
    {
        var liste = bilder.ToList();
        if (liste.Count == 0)
        {
            return;
        }

        col.Item().Column(c =>
        {
            Overskrift(c, "Bilder", org);
            foreach (var rad in liste.Chunk(3))
            {
                c.Item().PaddingTop(6).Row(r =>
                {
                    r.Spacing(8);
                    foreach (var bilde in rad)
                    {
                        r.RelativeItem().Height(130).Image(PdfBilde.Forminsk(bilde)).FitArea();
                    }
                    for (var i = rad.Length; i < 3; i++)
                    {
                        r.RelativeItem();
                    }
                });
            }
        });
    }

    // Signaturlinjer - den som utførte jobben og dato fylles ut, kunden kvitterer for hånd.
    public static void Signatur(ColumnDescriptor col, string? utfortAv, DateTime? dato, string? kontakt)
    {
        var venstre = string.Join(", ", new[]
        {
            string.IsNullOrWhiteSpace(utfortAv) ? "Utført av" : $"Utført av {utfortAv}",
            dato?.ToString("dd.MM.yyyy")
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var hoyre = $"Kvittert av kunde{(string.IsNullOrWhiteSpace(kontakt) ? "" : $" · {kontakt}")}";

        col.Item().PaddingTop(18).ShowEntire().Row(r =>
        {
            r.Spacing(30);
            foreach (var tekst in new[] { venstre, hoyre })
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Height(26);
                    c.Item().LineHorizontal(0.75f).LineColor(Svak);
                    c.Item().PaddingTop(3).Text(tekst).FontSize(8.5f).FontColor(Dempet);
                });
            }
        });
    }

    // Egen forside (mal "Klassisk med forside"): logo og nummer øverst, liten
    // overlinje, stor tittel, kunde/adresse og forsideteksten fra editoren
    // (med skriftstørrelse, fet, lister, linjeavstand osv.), og en faktarad
    // nederst over bunnteksten. Detaljene kommer på neste side.
    public static void Forside(PageDescriptor page, RapportProfil org, PdfLogo pdfLogo, string overlinje, string tittel,
        string? undertittel, string nummer, string? forsideHtml, params (string Label, string? Verdi)[] fakta)
    {
        Side(page);
        page.Background().Column(bg => bg.Item().Height(298).Background(org.AksentLys));

        page.Content().Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(e => org.TegnLogo(e, pdfLogo));
                row.AutoItem().AlignMiddle()
                    .Background(Colors.White).Border(0.75f).BorderColor(Linje).PaddingVertical(3).PaddingHorizontal(9)
                    .Text(nummer).FontSize(8.5f).Bold().FontColor(org.Aksent);
            });

            col.Item().PaddingTop(130).Text(overlinje.ToUpperInvariant()).FontSize(9).Bold().FontColor(org.Aksent).LetterSpacing(0.14f);
            col.Item().PaddingTop(8).Text(tittel).FontSize(28).Bold().FontColor(PdfStil.Ink).LineHeight(1.1f);
            if (!string.IsNullOrWhiteSpace(undertittel))
            {
                col.Item().PaddingTop(8).Text(undertittel).FontSize(11).FontColor(Dempet);
            }

            if (!string.IsNullOrWhiteSpace(forsideHtml))
            {
                col.Item().PaddingTop(26).PaddingRight(70).DefaultTextStyle(x => x.FontSize(10.5f))
                    .Column(tekst => ForsideRenderer.Render(tekst, forsideHtml));
            }
        });

        page.Footer().Column(f =>
        {
            var utfylte = fakta.Where(x => !string.IsNullOrWhiteSpace(x.Verdi)).ToList();
            if (utfylte.Count > 0)
            {
                f.Item().PaddingBottom(18).BorderTop(2).BorderColor(org.Aksent).PaddingTop(10).Row(row =>
                {
                    foreach (var (label, verdi) in utfylte)
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).SemiBold().FontColor(Svak).LetterSpacing(0.05f);
                            c.Item().PaddingTop(2).Text(verdi).FontSize(10.5f).Bold();
                        });
                    }
                });
            }
            f.Item().Element(e => Bunn(e, org));
        });
    }

    public static void Bunn(IContainer footer, RapportProfil org)
    {
        footer.PaddingTop(8).BorderTop(0.75f).BorderColor(Linje).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(org.Bunntekst).FontSize(7.5f).FontColor(Svak);
            row.ConstantItem(60).AlignRight().Text(t =>
            {
                t.Span("Side ").FontSize(7.5f).FontColor(Svak);
                t.CurrentPageNumber().FontSize(7.5f).FontColor(Svak);
                t.Span(" av ").FontSize(7.5f).FontColor(Svak);
                t.TotalPages().FontSize(7.5f).FontColor(Svak);
            });
        });
    }
}

// Avsenderen på rapporten: organisasjonen som er logget inn. Standard-
// organisasjonen (og manglende felt) faller tilbake til FirmaInfo og
// itlock-logoen, slik rapportene så ut før.
public sealed class RapportProfil
{
    public required string Navn { get; init; }
    public required string Bunntekst { get; init; }
    public required Color Aksent { get; init; }
    public required Color AksentLys { get; init; }
    private Tenant? Tenant { get; init; }

    public static RapportProfil Fra(Tenant? t)
    {
        var tema = OrganisasjonTema.FraId(t?.Tema);
        var brukFirmaInfo = t is null || t.ErStandard;

        var navn = !string.IsNullOrWhiteSpace(t?.PortalVisningsnavn) ? t!.PortalVisningsnavn!
            : !brukFirmaInfo && !string.IsNullOrWhiteSpace(t?.Navn) ? t!.Navn
            : FirmaInfo.Navn;
        var adresse = !string.IsNullOrWhiteSpace(t?.Adresse)
            ? string.Join(", ", new[] { t!.Adresse, string.Join(" ", new[] { t.Postnr, t.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : brukFirmaInfo ? FirmaInfo.AdresseFull : null;
        var telefon = !string.IsNullOrWhiteSpace(t?.Telefon) ? t!.Telefon : brukFirmaInfo ? FirmaInfo.Telefon : null;
        var epost = !string.IsNullOrWhiteSpace(t?.Epost) ? t!.Epost : brukFirmaInfo ? FirmaInfo.Epost : null;
        var orgNr = !string.IsNullOrWhiteSpace(t?.OrgNr) ? t!.OrgNr : null;

        var bunntekst = string.Join(" · ", new[]
        {
            navn, orgNr is null ? null : $"Org.nr {orgNr}", adresse, telefon is null ? null : $"Tlf {telefon}", epost
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new RapportProfil
        {
            Navn = navn,
            Bunntekst = bunntekst,
            Aksent = Color.FromHex(tema.Primaer),
            AksentLys = Color.FromHex(tema.PrimaerLys),
            Tenant = t
        };
    }

    public void TegnLogo(IContainer container, PdfLogo standardLogo)
    {
        if (Tenant?.LogoData is { Length: > 0 } data)
        {
            var boks = container.Height(30).AlignLeft();
            if ((Tenant.LogoContentType ?? "").Contains("svg", StringComparison.OrdinalIgnoreCase))
            {
                boks.Svg(System.Text.Encoding.UTF8.GetString(data)).FitHeight();
            }
            else
            {
                boks.Image(data).FitHeight();
            }
            return;
        }

        if (Tenant is null || Tenant.ErStandard)
        {
            container.AlignLeft().Element(e => standardLogo.Render(e, 24));
            return;
        }

        // Ingen opplastet logo: firmanavnet i organisasjonens farge.
        container.Text(Navn).FontSize(14).Bold().FontColor(Aksent);
    }
}
