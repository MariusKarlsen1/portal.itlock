using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Felles byggesteiner for rapportmalen «Sandbånd» (oktober 2026), brukt av
// arbeidsrapporten og servicerapporten slik at de ser like ut: topp med logo,
// tittel, rapportnummer og status i et lyst sandbånd, nøkkelinfo i et rolig rutenett, seksjoner med
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

    // Margene på sider satt opp med Side() - båndet trekkes ut over dem.
    public const float MargH = 1.8f * 28.3465f;
    public const float MargTopp = 1.6f * 28.3465f;

    // Sandbåndet (malen «Sandbånd», oktober 2026): organisasjonens lyse farge
    // helt ut til kantene med en strek i hovedfargen under. Logo og eventuelt
    // nummer/dato/status øverst, liten etikett, stor tittel, undertittel og en
    // rad med nøkkeltall. Legges som første element øverst på siden; margH og
    // margTopp er sidens marger, slik at båndet når helt ut til papirkanten.
    public static void Band(IContainer container, RapportProfil org, PdfLogo pdfLogo, string etikett, string tittel,
        string? undertittel, (string Label, string? Verdi)[]? fakta = null, string? hoyre = null,
        string? status = null, bool statusOk = false, float margH = MargH, float margTopp = MargTopp)
    {
        container.PaddingHorizontal(-margH).PaddingTop(-margTopp)
            .Background(org.AksentLys).BorderBottom(2.5f).BorderColor(org.Aksent)
            .PaddingHorizontal(margH).PaddingTop(margTopp + 4).PaddingBottom(18)
            .Column(b =>
            {
                b.Item().Row(row =>
                {
                    row.RelativeItem().Element(e => org.TegnLogo(e, pdfLogo, 26));
                    if (!string.IsNullOrWhiteSpace(hoyre) || !string.IsNullOrWhiteSpace(status))
                    {
                        row.AutoItem().AlignMiddle().Column(h =>
                        {
                            if (!string.IsNullOrWhiteSpace(hoyre))
                            {
                                h.Item().AlignRight().Text(hoyre).FontSize(8.5f).FontColor(Dempet);
                            }
                            if (!string.IsNullOrWhiteSpace(status))
                            {
                                h.Item().PaddingTop(4).AlignRight()
                                    .Background(statusOk ? GronnLys : Colors.White).PaddingVertical(2).PaddingHorizontal(8)
                                    .Text(statusOk ? $"✓ {status}" : status).FontSize(8).Bold().FontColor(statusOk ? Gronn : org.Aksent);
                            }
                        });
                    }
                });
                b.Item().PaddingTop(22).Text(etikett.ToUpperInvariant()).FontSize(7).Bold().FontColor(org.Aksent).LetterSpacing(0.08f);
                b.Item().PaddingTop(3).Text(tittel).FontSize(22).SemiBold().FontColor(PdfStil.Ink).LineHeight(1.1f);
                if (!string.IsNullOrWhiteSpace(undertittel))
                {
                    b.Item().PaddingTop(3).Text(undertittel).FontSize(10.5f).FontColor(Dempet);
                }

                var utfylte = (fakta ?? []).Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
                if (utfylte.Count > 0)
                {
                    b.Item().PaddingTop(16).Inlined(i =>
                    {
                        i.Spacing(10);
                        i.HorizontalSpacing(28);
                        foreach (var (label, verdi) in utfylte)
                        {
                            i.Item().Column(c =>
                            {
                                c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).Bold().FontColor(Svak).LetterSpacing(0.07f);
                                c.Item().PaddingTop(1).Text(verdi).FontSize(9.5f).SemiBold();
                            });
                        }
                    });
                }
            });
    }

    // Diskret topp på side 2 og videre: tittel til venstre, nummer til høyre og en tynn linje.
    public static void Lopetopp(IContainer container, RapportProfil org, string tittel, string? hoyre) =>
        container.PaddingBottom(4).BorderBottom(0.75f).BorderColor(Linje).PaddingBottom(5).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span(org.Navn).FontSize(8).Bold().FontColor(org.Aksent);
                t.Span($"  ·  {tittel}").FontSize(8).FontColor(Dempet);
            });
            if (!string.IsNullOrWhiteSpace(hoyre))
            {
                row.AutoItem().Text(hoyre).FontSize(8).FontColor(Dempet);
            }
        });

    // Toppen på rapportene: sandbåndet på første side, en diskret løpetopp på resten.
    // Legges i page.Header().
    public static void Topp(ColumnDescriptor col, RapportProfil org, PdfLogo pdfLogo, string tittel, string? undertittel,
        string nummer, DateTime dato, bool erUtfort, string statusTekst, params (string Label, string? Verdi)[] fakta)
    {
        var hoyre = $"{nummer} · {dato:dd.MM.yyyy}";
        col.Item().ShowOnce().Element(e => Band(e, org, pdfLogo, tittel, undertittel ?? tittel, null, fakta, hoyre,
            erUtfort ? "Utført" : statusTekst, erUtfort));
        col.Item().SkipOnce().Element(e => Lopetopp(e, org, tittel, hoyre));
    }

    // Nøkkelinfo i et rolig rutenett med tynne linjer (ingen fylt boks).
    public static void InfoBoks(ColumnDescriptor col, RapportProfil org, params (string Label, string? Verdi)[] felter)
    {
        var utfylte = felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();
        if (utfylte.Count == 0)
        {
            return;
        }

        col.Item().BorderBottom(0.75f).BorderColor(Linje).PaddingBottom(6).Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
            foreach (var (label, verdi) in utfylte)
            {
                table.Cell().PaddingVertical(4).PaddingRight(10).Column(c =>
                {
                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).Bold().FontColor(Svak).LetterSpacing(0.07f);
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
            Tekst(c.Item().PaddingTop(4), tekst);
        });
    }

    // Ren tekst som før, eller formatert tekst fra sluttrapporten (RikTekst):
    // stor tekst, overskrift, fet/kursiv/understrek og lister.
    private static void Tekst(IContainer container, string tekst)
    {
        if (!RikTekst.ErFormatert(tekst))
        {
            container.Text(tekst);
            return;
        }

        container.Column(c =>
        {
            c.Spacing(3);
            foreach (var b in RikTekst.Blokker(tekst))
            {
                void Biter(TextDescriptor t)
                {
                    foreach (var bit in b.Biter)
                    {
                        var span = t.Span(bit.Tekst);
                        if (bit.Fet) span.Bold();
                        if (bit.Kursiv) span.Italic();
                        if (bit.Understrek) span.Underline();
                    }
                }

                switch (b.Type)
                {
                    case RikTekst.BlokkType.Overskrift:
                        c.Item().PaddingTop(2).Text(t => { t.DefaultTextStyle(s => s.FontSize(13).Bold()); Biter(t); });
                        break;
                    case RikTekst.BlokkType.Stor:
                        c.Item().Text(t => { t.DefaultTextStyle(s => s.FontSize(11.5f).SemiBold()); Biter(t); });
                        break;
                    case RikTekst.BlokkType.Punkt or RikTekst.BlokkType.Nummer:
                        c.Item().PaddingLeft(6).Row(r =>
                        {
                            r.ConstantItem(14).Text(b.Type == RikTekst.BlokkType.Punkt ? "•" : $"{b.Nummer}.");
                            r.RelativeItem().Text(Biter);
                        });
                        break;
                    default:
                        c.Item().Text(Biter);
                        break;
                }
            }
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
            Tekst(c.Item().PaddingTop(5).Background(GulLys).BorderLeft(3).BorderColor(Gul)
                .PaddingVertical(8).PaddingHorizontal(11), tekst);
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

    // Avsenderens kontaktdetaljer hver for seg (null når de ikke er registrert) -
    // brukt der malen viser dem i egne felt, f.eks. forsiden på tilbud.
    public string? Adresse { get; init; }
    public string? Telefon { get; init; }
    public string? Epost { get; init; }
    public string? OrgNr { get; init; }
    public bool ErStandard { get; init; }
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
            Tenant = t,
            Adresse = adresse,
            Telefon = telefon,
            Epost = epost,
            OrgNr = orgNr,
            ErStandard = brukFirmaInfo
        };
    }

    public void TegnLogo(IContainer container, PdfLogo standardLogo, float hoyde = 30)
    {
        if (Tenant?.LogoData is { Length: > 0 } data)
        {
            var boks = container.Height(hoyde).AlignLeft();
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
            container.AlignLeft().Element(e => standardLogo.Render(e, hoyde * 0.8f));
            return;
        }

        // Ingen opplastet logo: firmanavnet i organisasjonens farge.
        container.Text(Navn).FontSize(hoyde * 0.47f).Bold().FontColor(Aksent);
    }
}
