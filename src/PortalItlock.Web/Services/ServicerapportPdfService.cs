using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Servicerapport for en servicerunde: forside i samme stil som FDV (grå kolonne
// til venstre med logo og kontaktinfo) og en oppsummering med det som bør følges
// opp og neste service i et grått felt øverst, deretter utført arbeid, byttede
// deler, sjekkliste, bilder og signaturer. Enkel og nøytral, slik at den passer
// for alle organisasjoner: bare logoen har farge, og deler uten innhold faller bort.
public class ServicerapportPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly Color Graa = Color.FromHex("#F0F1F3");
    private static readonly Color Skille = Color.FromHex("#D3D5D8");
    private static readonly Color Linje = Color.FromHex("#E7E8EA");
    private static readonly Color Dempet = Color.FromHex("#55554F");
    private static readonly Color Svak = Color.FromHex("#6B6B68");
    private static readonly Color Brodtekst = Color.FromHex("#3B3B39");

    public async Task<byte[]?> GenerateAsync(int servicerundeId)
    {
        var runde = await db.Servicerunder
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Prosjektleder)
            .Include(r => r.UtfortAvBruker)
            .Include(r => r.Deler).ThenInclude(d => d.Dor)
            .Include(r => r.Sjekkpunkter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == servicerundeId);

        if (runde is null)
        {
            return null;
        }

        runde.Media = await db.ServicerundeMedia
            .Where(m => m.ServicerundeId == servicerundeId && m.ContentType.StartsWith("image/"))
            .OrderBy(m => m.OpprettetDato)
            .ToListAsync();

        return Bygg(runde, RapportProfil.Fra(tenantContext.Current), pdfLogo);
    }

    public static byte[] Bygg(Servicerunde runde, RapportProfil org, PdfLogo pdfLogo)
    {
        var dato = runde.Dato.ToString("dd.MM.yyyy");
        var erUtfort = !runde.ErPlanlagt;
        var tittel = !string.IsNullOrWhiteSpace(runde.Prosjekt?.Navn) ? runde.Prosjekt.Navn : "Servicerapport";
        var nesteService = runde.NesteServiceDato?.ToString("dd.MM.yyyy");

        var document = Document.Create(doc =>
        {
            doc.Page(page => Forside(page, runde, org, pdfLogo, tittel, dato, nesteService, erUtfort));

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginTop(34);
                page.MarginBottom(24);
                page.DefaultTextStyle(x => x.FontSize(10.4f).FontColor(PdfStil.Ink).LineHeight(1.42f));

                page.Content().Column(col =>
                {
                    col.Item().Text("Oppsummering").FontSize(20).Bold().LineHeight(1.2f);
                    col.Item().PaddingTop(2).Text($"{tittel} · {dato}").FontSize(9.6f).FontColor(Dempet);

                    Oppfolging(col, runde.Anbefalinger, nesteService);

                    if (!string.IsNullOrWhiteSpace(runde.StatusBeskrivelse))
                    {
                        col.Item().PaddingTop(18).ShowEntire().Column(c =>
                        {
                            Overskrift(c, erUtfort ? "Utført arbeid" : "Planlagt arbeid");
                            RapportMal.Tekst(c.Item().PaddingTop(4).DefaultTextStyle(x => x.FontColor(Brodtekst)), runde.StatusBeskrivelse);
                        });
                    }

                    ByttedeDeler(col, runde.Deler);
                    Sjekkliste(col, runde.Sjekkpunkter.OrderBy(p => p.Rekkefolge).ToList(), erUtfort);
                    Bilder(col, runde.Media.Select(m => m.Data).ToList());
                    Signatur(col, runde, org, dato);
                });

                Bunn(page.Footer(), org);
            });
        });

        return document.GeneratePdf();
    }

    private static void Forside(PageDescriptor page, Servicerunde runde, RapportProfil org, PdfLogo pdfLogo,
        string tittel, string dato, string? nesteService, bool erUtfort)
    {
        var prosjekt = runde.Prosjekt;
        var kunde = prosjekt?.Kunde;
        var adresse = !string.IsNullOrWhiteSpace(prosjekt?.Adresse) ? prosjekt.Adresse : kunde?.Adresse;
        var kontaktperson = prosjekt?.Prosjektleder ?? runde.UtfortAvBruker;
        var kontaktNavn = kontaktperson?.Navn ?? (org.ErStandard ? FirmaInfo.Kontaktperson : null);
        var kontaktEpost = kontaktperson?.Epost ?? org.Epost;

        page.Size(PageSizes.A4);
        page.Margin(0);
        page.DefaultTextStyle(x => x.FontSize(10).FontColor(PdfStil.Ink).LineHeight(1.35f));
        page.Background().AlignLeft().Width(160).Background(Graa).Extend();

        page.Content().Row(row =>
        {
            row.ConstantItem(160).PaddingVertical(42).PaddingHorizontal(20).Column(kol =>
            {
                kol.Item().Element(e => org.TegnLogo(e, pdfLogo, 26));
                kol.Item().ExtendVertical().AlignBottom().Column(bunn =>
                {
                    if (!string.IsNullOrWhiteSpace(kontaktNavn))
                    {
                        Etikett(bunn, "Kontaktperson");
                        bunn.Item().PaddingTop(2).Text(kontaktNavn).FontSize(9.5f).SemiBold();
                        KontaktLinje(bunn, kontaktEpost);
                        bunn.Item().Height(12);
                    }

                    if (!string.IsNullOrWhiteSpace(org.Telefon) || !string.IsNullOrWhiteSpace(org.Epost))
                    {
                        Etikett(bunn, "Service og feil");
                        bunn.Item().Height(2);
                        KontaktLinje(bunn, org.Telefon);
                        KontaktLinje(bunn, org.Epost);
                        bunn.Item().Height(12);
                    }

                    Etikett(bunn, "Firma");
                    bunn.Item().Height(2);
                    KontaktLinje(bunn, org.Navn);
                    KontaktLinje(bunn, org.Adresse);
                    KontaktLinje(bunn, org.OrgNr is null ? null : $"Org.nr {org.OrgNr}");
                });
            });

            row.RelativeItem().PaddingVertical(42).PaddingHorizontal(40).Column(kol =>
            {
                kol.Item().Height(150);
                Etikett(kol, "Servicerapport");
                kol.Item().PaddingTop(6).Text(tittel).FontSize(28).Bold().LineHeight(1.15f);
                kol.Item().PaddingTop(4).Text($"{(erUtfort ? "Service" : "Planlagt service")} {dato}").FontSize(12).FontColor(Dempet);

                if (!string.IsNullOrWhiteSpace(kunde?.Navn))
                {
                    kol.Item().PaddingTop(45).Column(c =>
                    {
                        Etikett(c, "Kunde");
                        c.Item().PaddingTop(2).Text(kunde.Navn).FontSize(12).SemiBold();
                        if (!string.IsNullOrWhiteSpace(adresse))
                        {
                            c.Item().Text(adresse).FontSize(9).FontColor(Dempet);
                        }
                    });
                }

                kol.Item().PaddingTop(20).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    var felter = new (string Etikett, string? Verdi)[]
                    {
                        (erUtfort ? "Utført" : "Planlagt", dato),
                        ("Utført av", runde.UtfortAvBruker?.Navn),
                        ("Neste service", nesteService),
                        ("Rapport nr.", runde.Id.ToString())
                    };
                    foreach (var (etikett, verdi) in felter.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)))
                    {
                        t.Cell().PaddingBottom(10).Column(c =>
                        {
                            Etikett(c, etikett);
                            c.Item().PaddingTop(1).Text(verdi).FontSize(10.4f).SemiBold();
                        });
                    }
                });

                if (!string.IsNullOrWhiteSpace(runde.Forside))
                {
                    kol.Item().PaddingTop(10).BorderTop(0.75f).BorderColor(Skille).PaddingTop(10)
                        .DefaultTextStyle(x => x.FontColor(Brodtekst))
                        .Column(tekst => ForsideRenderer.Render(tekst, runde.Forside));
                }
            });
        });
    }

    // Grått felt øverst. Det vises bare når det finnes noe å følge opp eller en
    // neste service, og tar da bare med det som faktisk er fylt ut.
    private static void Oppfolging(ColumnDescriptor col, string? anbefalinger, string? nesteService)
    {
        var harAnbefaling = !string.IsNullOrWhiteSpace(anbefalinger);
        if (!harAnbefaling && string.IsNullOrWhiteSpace(nesteService))
        {
            return;
        }

        col.Item().PaddingTop(14).Background(Graa).PaddingVertical(15).PaddingHorizontal(16).Row(row =>
        {
            if (harAnbefaling)
            {
                row.RelativeItem().Column(c =>
                {
                    Etikett(c, "Bør følges opp");
                    RapportMal.Tekst(c.Item().PaddingTop(3).DefaultTextStyle(x => x.FontSize(11).Medium()), anbefalinger!);
                });
            }

            if (!string.IsNullOrWhiteSpace(nesteService))
            {
                var neste = harAnbefaling
                    ? row.ConstantItem(125).PaddingLeft(16).BorderLeft(0.75f).BorderColor(Skille).PaddingLeft(16)
                    : row.RelativeItem();
                neste.Column(c =>
                {
                    Etikett(c, "Neste service");
                    c.Item().PaddingTop(3).Text(nesteService).FontSize(13).Bold();
                });
            }
        });
    }

    private static void ByttedeDeler(ColumnDescriptor col, ICollection<ServicerundeDel> deler)
    {
        if (deler.Count == 0)
        {
            return;
        }

        col.Item().PaddingTop(16).Column(c =>
        {
            Overskrift(c, "Byttede deler");
            c.Item().PaddingTop(4).DefaultTextStyle(x => x.FontSize(9.4f)).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(60);
                    cd.RelativeColumn(2);
                    cd.RelativeColumn(3);
                });

                table.Header(h =>
                {
                    foreach (var kolonne in new[] { "Dør", "Hva ble byttet", "Hva var galt" })
                    {
                        h.Cell().BorderBottom(0.75f).BorderColor(PdfStil.Ink).PaddingVertical(3.5f).PaddingRight(8)
                            .Text(kolonne).FontSize(8).SemiBold().FontColor(Svak);
                    }
                });

                foreach (var del in deler.OrderBy(d => d.Dato))
                {
                    Celle(table).Text(del.Dor?.Dornummer ?? "–").SemiBold();
                    Celle(table).Text(del.Beskrivelse);
                    Celle(table).Text(string.IsNullOrWhiteSpace(del.Feil) ? "–" : del.Feil);
                }
            });
        });
    }

    private static IContainer Celle(TableDescriptor table) =>
        table.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4.5f).PaddingRight(8);

    private static void Sjekkliste(ColumnDescriptor col, List<ServicerundeSjekkpunkt> punkter, bool erUtfort)
    {
        if (punkter.Count == 0)
        {
            return;
        }

        col.Item().PaddingTop(16).Column(c =>
        {
            Overskrift(c, "Sjekkliste");
            c.Item().PaddingTop(3).DefaultTextStyle(x => x.FontSize(9.4f)).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn();
                    cd.ConstantColumn(20);
                    cd.RelativeColumn();
                });

                for (var i = 0; i < punkter.Count; i++)
                {
                    if (i % 2 == 1)
                    {
                        table.Cell();
                    }

                    var punkt = punkter[i];
                    table.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(3.75f).Row(r =>
                    {
                        r.ConstantItem(14).Text(punkt.Fullfort ? "✓" : "–").Bold();
                        r.RelativeItem().Text(t =>
                        {
                            t.Span(punkt.Tekst).FontColor(punkt.Fullfort ? PdfStil.Ink : Dempet);
                            if (!punkt.Fullfort && erUtfort)
                            {
                                t.Span(" · Ikke utført").FontColor(Dempet);
                            }
                        });
                    });
                }
            });
        });
    }

    private static void Bilder(ColumnDescriptor col, List<byte[]> bilder)
    {
        if (bilder.Count == 0)
        {
            return;
        }

        var rader = bilder.Chunk(3).ToList();
        for (var i = 0; i < rader.Count; i++)
        {
            var rad = rader[i];
            col.Item().PaddingTop(i == 0 ? 16 : 7.5f).ShowEntire().Column(c =>
            {
                if (i == 0)
                {
                    Overskrift(c, "Bilder");
                }

                c.Item().PaddingTop(i == 0 ? 5 : 0).Row(r =>
                {
                    r.Spacing(7.5f);
                    foreach (var bilde in rad)
                    {
                        r.RelativeItem().Height(112).Image(PdfBilde.Forminsk(bilde)).FitArea();
                    }

                    for (var tom = rad.Length; tom < 3; tom++)
                    {
                        r.RelativeItem();
                    }
                });
            });
        }
    }

    // Står nederst på siste side: ExtendVertical tar resten av siden, og får
    // signaturene ikke plass der, flyttes de samlet til en ny side.
    private static void Signatur(ColumnDescriptor col, Servicerunde runde, RapportProfil org, string dato)
    {
        var prosjekt = runde.Prosjekt;
        var kunde = prosjekt?.Kunde;
        var kontakt = !string.IsNullOrWhiteSpace(prosjekt?.Kontaktperson) ? prosjekt.Kontaktperson : kunde?.Kontaktperson;
        var utfort = string.Join(" · ", new[]
        {
            string.Join(", ", new[] { runde.UtfortAvBruker?.Navn, org.Navn }.Where(s => !string.IsNullOrWhiteSpace(s))),
            dato
        });
        var kvittert = string.Join(", ", new[] { kontakt, kunde?.Navn }.Where(s => !string.IsNullOrWhiteSpace(s)));

        col.Item().ExtendVertical().AlignBottom().PaddingTop(20).PaddingBottom(14).ShowEntire().Row(r =>
        {
            r.Spacing(30);
            foreach (var tekst in new[] { utfort, string.IsNullOrWhiteSpace(kvittert) ? "Kunde" : kvittert })
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Height(28);
                    c.Item().LineHorizontal(0.75f).LineColor(PdfStil.Ink);
                    c.Item().PaddingTop(3).Text(tekst).FontSize(8.4f).FontColor(Svak);
                });
            }
        });
    }

    private static void Bunn(IContainer footer, RapportProfil org) => footer.Row(row =>
    {
        row.RelativeItem().Text(org.Bunntekst).FontSize(7.8f).FontColor(Svak);
        row.ConstantItem(70).AlignRight().Text(t =>
        {
            t.DefaultTextStyle(x => x.FontSize(7.8f).FontColor(Svak));
            t.Span("Side ");
            t.CurrentPageNumber();
            t.Span(" av ");
            t.TotalPages();
        });
    });

    private static void Overskrift(ColumnDescriptor col, string tekst) =>
        col.Item().Text(tekst).FontSize(10.6f).Bold();

    private static void Etikett(ColumnDescriptor col, string tekst) =>
        col.Item().Text(tekst.ToUpperInvariant()).FontSize(7.4f).Bold().FontColor(Dempet).LetterSpacing(0.05f);

    private static void KontaktLinje(ColumnDescriptor col, string? tekst)
    {
        if (!string.IsNullOrWhiteSpace(tekst))
        {
            col.Item().Text(tekst).FontSize(8.8f).FontColor(Dempet);
        }
    }
}
