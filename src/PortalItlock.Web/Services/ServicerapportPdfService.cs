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
// deler, sjekkliste, bilder og signaturer. Er det mange punkter å følge opp, står
// bare de kritiske i det grå feltet, og alle punktene får egne sider bakerst.
// Enkel og nøytral, slik at den passer for alle organisasjoner: bare logoen har
// farge, og deler uten innhold faller bort.
public class ServicerapportPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly Color Graa = Color.FromHex("#F0F1F3");
    private static readonly Color Skille = Color.FromHex("#D3D5D8");
    private static readonly Color Linje = Color.FromHex("#E7E8EA");
    private static readonly Color Dempet = Color.FromHex("#55554F");
    private static readonly Color Svak = Color.FromHex("#6B6B68");
    private static readonly Color Brodtekst = Color.FromHex("#3B3B39");

    // Tegnes som figur: skriften i PDF-en har ikke tegnet for hake.
    private const string Hake = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M4 13l5 6L20 6' fill='none' stroke='#292927' stroke-width='3' stroke-linecap='round' stroke-linejoin='round'/></svg>";
    private const string PunktSeksjon = "punkter";

    public async Task<byte[]?> GenerateAsync(int servicerundeId)
    {
        // Lest uten sporing: rapporten legger til midlertidige rader (byttede deler
        // fra komponentloggen) som aldri skal lagres.
        var runde = await db.Servicerunder.AsNoTracking()
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(r => r.Prosjekt).ThenInclude(p => p!.Prosjektleder)
            .Include(r => r.UtfortAvBruker)
            .Include(r => r.Deler).ThenInclude(d => d.Dor)
            .Include(r => r.Sjekkpunkter)
            .Include(r => r.Dorer).ThenInclude(d => d.Punkter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == servicerundeId);

        if (runde is null)
        {
            return null;
        }

        // Dør for dør: deler som er byttet på dørene mens runden pågikk (komponentloggen).
        if (runde.DorForDor && runde.Dorer.Count > 0)
        {
            var dorIder = runde.Dorer.Select(d => d.DorId).ToList();
            var fra = runde.Dato.Date;
            var til = runde.FullfortDato ?? DateTime.Now;
            var bytter = await db.DorKomponentLogger.AsNoTracking()
                .Where(l => dorIder.Contains(l.DorId) && l.Hendelse == DorKomponentHendelse.Fjernet && l.Tidspunkt >= fra && l.Tidspunkt <= til && l.Merknad != null)
                .ToListAsync();
            foreach (var b in bytter)
            {
                var dn = runde.Dorer.First(d => d.DorId == b.DorId).Dornummer;
                runde.Deler.Add(new ServicerundeDel { Beskrivelse = b.Merknad!, Dor = new Dor { Dornummer = dn }, Dato = b.Tidspunkt });
            }
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
        var anbefalinger = ServiceAnbefalinger.Tolk(runde.Anbefalinger);

        var document = Document.Create(doc =>
        {
            doc.Page(page => Forside(page, runde, org, pdfLogo, tittel, dato, nesteService, erUtfort));

            doc.Page(page =>
            {
                Innholdsside(page, org);

                page.Content().Column(col =>
                {
                    col.Item().Text("Oppsummering").FontSize(20).Bold().LineHeight(1.2f);
                    col.Item().PaddingTop(2).Text($"{tittel} · {dato}").FontSize(9.6f).FontColor(Dempet);

                    Oppfolging(col, runde.Anbefalinger, anbefalinger, nesteService);

                    if (!string.IsNullOrWhiteSpace(runde.StatusBeskrivelse))
                    {
                        col.Item().PaddingTop(18).EnsureSpace(60).Column(c =>
                        {
                            Overskrift(c, erUtfort ? "Utført arbeid" : "Planlagt arbeid");
                            RapportMal.Tekst(c.Item().PaddingTop(4).DefaultTextStyle(x => x.FontColor(Brodtekst)), ServiceAnbefalinger.MedPunkter(runde.StatusBeskrivelse));
                        });
                    }

                    if (runde.DorForDor && runde.Dorer.Count > 0)
                    {
                        DorForDor(col, runde.Dorer.OrderBy(d => d.Rekkefolge).ToList());
                    }
                    ByttedeDeler(col, runde.Deler);
                    Sjekkliste(col, runde.Sjekkpunkter.OrderBy(p => p.Rekkefolge).ToList(), erUtfort);
                    Bilder(col, runde.Media.Select(m => m.Data).ToList());
                    Signatur(col, runde, org, dato);
                });
            });

            if (anbefalinger.HarEgneSider)
            {
                doc.Page(page =>
                {
                    Innholdsside(page, org);
                    page.Content().Column(col => PunktSider(col, anbefalinger.Linjer, tittel, dato));
                });
            }
        });

        return document.GeneratePdf();
    }

    private static void Innholdsside(PageDescriptor page, RapportProfil org)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(40);
        page.MarginTop(34);
        page.MarginBottom(24);
        page.DefaultTextStyle(x => x.FontSize(10.4f).FontColor(PdfStil.Ink).LineHeight(1.42f));
        Bunn(page.Footer(), org);
    }

    private static void Forside(PageDescriptor page, Servicerunde runde, RapportProfil org, PdfLogo pdfLogo,
        string tittel, string dato, string? nesteService, bool erUtfort)
    {
        var prosjekt = runde.Prosjekt;
        var kunde = prosjekt?.Kunde;
        var adresse = !string.IsNullOrWhiteSpace(prosjekt?.Adresse) ? prosjekt.Adresse : kunde?.Adresse;
        var kundeKontakt = !string.IsNullOrWhiteSpace(prosjekt?.Kontaktperson) ? prosjekt.Kontaktperson : kunde?.Kontaktperson;

        // Mangler kundenavnet, vises det vi har om kunden i stedet.
        var (kundeEtikett, kundeLinje) = !string.IsNullOrWhiteSpace(kunde?.Navn)
            ? ("Kunde", kunde.Navn)
            : !string.IsNullOrWhiteSpace(kundeKontakt)
                ? ("Kontaktperson hos kunden", kundeKontakt)
                : ("Adresse", adresse);
        var kundeAdresse = kundeEtikett == "Adresse" ? null : adresse;
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

                if (!string.IsNullOrWhiteSpace(kundeLinje))
                {
                    kol.Item().PaddingTop(45).Column(c =>
                    {
                        Etikett(c, kundeEtikett);
                        c.Item().PaddingTop(2).Text(kundeLinje).FontSize(12).SemiBold();
                        if (!string.IsNullOrWhiteSpace(kundeAdresse))
                        {
                            c.Item().Text(kundeAdresse).FontSize(9).FontColor(Dempet);
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
    // neste service, og tar da bare med det som faktisk er fylt ut. Lang tekst
    // står ikke her: da vises antall punkter, hvor de står og det som er kritisk.
    private static void Oppfolging(ColumnDescriptor col, string? anbefalinger, ServiceAnbefalinger.Tolking tolking, string? nesteService)
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
                    if (tolking.ErLang)
                    {
                        Sammendrag(c, tolking);
                    }
                    else
                    {
                        RapportMal.Tekst(c.Item().PaddingTop(3).DefaultTextStyle(x => x.FontSize(11).Medium()), ServiceAnbefalinger.MedPunkter(anbefalinger!));
                    }
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

    private static void Sammendrag(ColumnDescriptor col, ServiceAnbefalinger.Tolking tolking)
    {
        if (tolking.HarEgneSider)
        {
            col.Item().PaddingTop(3).Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(11).Medium());
                t.Span(tolking.Antall switch
                {
                    0 => "Teksten står på side ",
                    1 => "1 punkt står på side ",
                    var n => $"{n} punkter står på side "
                });
                t.BeginPageNumberOfSection(PunktSeksjon);
                t.Span(".");
            });
        }

        if (tolking.Kritisk.Count == 0)
        {
            return;
        }

        if (tolking.HarEgneSider)
        {
            col.Item().Height(10);
        }

        Etikett(col, "Kritisk");
        foreach (var linje in tolking.Kritisk)
        {
            col.Item().PaddingTop(4).Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(10));
                if (linje.Dor is not null)
                {
                    t.Span(linje.Dor).Bold();
                    t.Span(" – ");
                }

                t.Span(linje.Tekst);
            });
        }
    }

    // Alle punktene: overskriftene fra teksten som grå felt, og en rad per dør.
    private static void PunktSider(ColumnDescriptor col, IReadOnlyList<ServiceAnbefalinger.Linje> linjer, string tittel, string dato)
    {
        col.Item().Section(PunktSeksjon).Text("Bør følges opp").FontSize(20).Bold().LineHeight(1.2f);
        col.Item().PaddingTop(2).PaddingBottom(4).Text($"{tittel} · {dato}").FontSize(9.6f).FontColor(Dempet);

        foreach (var linje in linjer)
        {
            switch (linje.Type)
            {
                // EnsureSpace holder overskriften sammen med raden under.
                case ServiceAnbefalinger.LinjeType.Overskrift:
                    col.Item().PaddingTop(12).EnsureSpace(60).Background(Graa).PaddingVertical(4.5f).PaddingHorizontal(7.5f)
                        .Text(linje.Tekst).FontSize(9.8f).Bold();
                    break;
                case ServiceAnbefalinger.LinjeType.Rad:
                    col.Item().EnsureSpace(36).BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4.5f).PaddingHorizontal(7.5f)
                        .DefaultTextStyle(x => x.FontSize(9.4f)).Row(r =>
                        {
                            r.ConstantItem(122).PaddingRight(10).Text(linje.Dor).Bold();
                            r.RelativeItem().Text(linje.Tekst);
                        });
                    break;
                default:
                    col.Item().PaddingVertical(5).PaddingHorizontal(7.5f)
                        .Text(linje.Tekst).FontSize(9.4f).FontColor(Brodtekst);
                    break;
            }
        }
    }

    // Fase 6: kontrolloversikt og avvik per dør for runder som føres dør for dør.
    private static void DorForDor(ColumnDescriptor col, List<ServicerundeDor> dorer)
    {
        var avvik = dorer.Where(d => d.Status == ServicerundeDorStatus.Avvik).ToList();
        if (avvik.Count > 0)
        {
            col.Item().PaddingTop(16).Column(c =>
            {
                Overskrift(c, "Avvik og anbefalte tiltak");
                c.Item().PaddingTop(4).DefaultTextStyle(x => x.FontSize(9.4f)).Table(table =>
                {
                    table.ColumnsDefinition(cd => { cd.ConstantColumn(60); cd.RelativeColumn(2); cd.RelativeColumn(3); });
                    table.Header(h =>
                    {
                        foreach (var kolonne in new[] { "Dør", "Kontrollpunkt", "Beskrivelse" })
                        {
                            h.Cell().BorderBottom(0.75f).BorderColor(PdfStil.Ink).PaddingVertical(3.5f).PaddingRight(8).Text(kolonne).FontSize(8).SemiBold().FontColor(Svak);
                        }
                    });
                    foreach (var d in avvik)
                    {
                        foreach (var p in d.Punkter.Where(p => p.Status == ServicerundeDorStatus.Avvik).OrderBy(p => p.Rekkefolge))
                        {
                            Celle(table).Text(d.Dornummer).SemiBold();
                            Celle(table).Text(p.Tekst);
                            Celle(table).Text(string.Join(" ", new[] { p.Kommentar, d.Notat }.Where(s => !string.IsNullOrWhiteSpace(s))) is { Length: > 0 } t ? t : "–");
                        }
                    }
                });
            });
        }

        col.Item().PaddingTop(16).Column(c =>
        {
            Overskrift(c, $"Kontrolloversikt – {dorer.Count} dører");
            c.Item().PaddingTop(4).DefaultTextStyle(x => x.FontSize(9.2f)).Table(table =>
            {
                table.ColumnsDefinition(cd => { cd.ConstantColumn(60); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.ConstantColumn(90); });
                table.Header(h =>
                {
                    foreach (var kolonne in new[] { "Dør", "Plassering", "Kategori", "Status" })
                    {
                        h.Cell().BorderBottom(0.75f).BorderColor(PdfStil.Ink).PaddingVertical(3.5f).PaddingRight(8).Text(kolonne).FontSize(8).SemiBold().FontColor(Svak);
                    }
                });
                foreach (var d in dorer)
                {
                    Celle(table).Text(d.Dornummer).SemiBold();
                    Celle(table).Text(string.Join(" · ", new[] { d.Rom, string.IsNullOrWhiteSpace(d.Etasje) ? null : $"{d.Etasje}. etg" }.Where(s => !string.IsNullOrWhiteSpace(s))) is { Length: > 0 } pl ? pl : "–");
                    Celle(table).Text(d.Kategori ?? "–");
                    Celle(table).Text(d.Status switch { ServicerundeDorStatus.Ok => "OK", ServicerundeDorStatus.Avvik => "Avvik", _ => "Ikke kontrollert" })
                        .FontColor(d.Status == ServicerundeDorStatus.Avvik ? Color.FromHex("#B42318") : PdfStil.Ink);
                }
            });
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
                        if (punkt.Fullfort)
                        {
                            r.ConstantItem(14).PaddingTop(2.25f).AlignLeft().Width(9).Height(9).Svg(Hake).FitArea();
                        }
                        else
                        {
                            r.ConstantItem(14).Text("–").Bold();
                        }

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
        var kvittert = !string.IsNullOrWhiteSpace(runde.KundeSignertAv)
            ? $"{runde.KundeSignertAv}{(kunde?.Navn is { Length: > 0 } kn ? $", {kn}" : "")} · {runde.KundeSignertDato:dd.MM.yyyy}"
            : string.Join(", ", new[] { kontakt, kunde?.Navn }.Where(s => !string.IsNullOrWhiteSpace(s)));

        col.Item().ExtendVertical().AlignBottom().PaddingTop(20).PaddingBottom(14).ShowEntire().Row(r =>
        {
            r.Spacing(30);
            var i = 0;
            foreach (var tekst in new[] { utfort, string.IsNullOrWhiteSpace(kvittert) ? "Kunde" : kvittert })
            {
                var erKunde = i++ == 1;
                r.RelativeItem().Column(c =>
                {
                    if (erKunde && runde.KundeSignatur is { Length: > 0 } sig)
                    {
                        c.Item().Height(42).AlignLeft().Image(sig).FitHeight();
                    }
                    else
                    {
                        c.Item().Height(28);
                    }
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
