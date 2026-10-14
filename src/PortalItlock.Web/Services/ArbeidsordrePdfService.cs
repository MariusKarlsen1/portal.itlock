using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Arbeidsrapport, mal "Klassisk" (oktober 2026): logo og rapportnummer øverst,
// nøkkelinfo i en rolig boks, deretter oppdraget, utført arbeid, anbefalinger,
// sjekkliste, materiell, bilder og signaturlinjer. Logo, navn, kontaktinfo og
// farge hentes fra organisasjonen som er logget inn (Tenant), slik at
// rapporten passer for alle organisasjoner i portalen. Priser og timer vises
// aldri. Tomme felt og seksjoner utelates.
public class ArbeidsordrePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly Color Dempet = Color.FromHex("#6D675F");
    private static readonly Color Svak = Color.FromHex("#8B857D");
    private static readonly Color Linje = Color.FromHex("#E6DFD4");
    private static readonly Color Gronn = Color.FromHex("#2E7D4F");
    private static readonly Color GronnLys = Color.FromHex("#E3F1E8");
    private static readonly Color Gul = Color.FromHex("#C9902F");
    private static readonly Color GulLys = Color.FromHex("#FDF6EA");

    public async Task<byte[]?> GenerateAsync(int arbeidsordreId)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.AnsvarligMontor)
            .Include(a => a.FullfortAvBruker)
            .Include(a => a.Sjekkpunkter)
            .Include(a => a.Varer)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId);

        if (ordre is null)
        {
            return null;
        }

        ordre.Media = await db.ArbeidsordreMedia
            .Where(m => m.ArbeidsordreId == arbeidsordreId && m.ContentType.StartsWith("image/"))
            .OrderBy(m => m.OpprettetDato)
            .ToListAsync();

        // Materiell: varelinjene fra tilbudet ordren er laget fra (det montøren
        // plukker), pluss varer lagt inn direkte på ordren. Kun navn og antall.
        var materiell = new List<(string Navn, string Antall)>();
        if (ordre.TilbudId.HasValue)
        {
            var linjer = await db.TilbudLinjer
                .Where(l => l.TilbudId == ordre.TilbudId.Value && !l.ErGruppering && l.LevertAv == LevertAv.F)
                .OrderBy(l => l.Rekkefolge)
                .ToListAsync();
            materiell.AddRange(linjer
                .Where(l => !ErArbeidslinje(l))
                .Select(l => (l.Navn, $"{l.Antall} {(string.IsNullOrWhiteSpace(l.Enhet) ? "stk" : l.Enhet)}")));
        }
        materiell.AddRange(ordre.Varer.OrderBy(v => v.Navn).Select(v => (v.Navn, $"{v.Antall} stk")));

        var org = Organisasjon.Fra(tenantContext.Current);
        var aksent = Color.FromHex(org.Farge);
        var aksentLys = Color.FromHex(org.FargeLys);

        var kunde = ordre.Kunde ?? ordre.Prosjekt?.Kunde;
        var kontakt = !string.IsNullOrWhiteSpace(ordre.Prosjekt?.Kontaktperson) ? ordre.Prosjekt!.Kontaktperson : kunde?.Kontaktperson;
        var adresse = string.Join(", ", new[]
        {
            ordre.Adresse,
            string.Join(" ", new[] { ordre.Postnr, ordre.Sted }.Where(s => !string.IsNullOrWhiteSpace(s)))
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(adresse))
        {
            adresse = ordre.Prosjekt?.Adresse ?? kunde?.Adresse ?? "";
        }

        var utfortDato = ordre.FullfortDato ?? ordre.PlanlagtDato;
        var utfortAv = ordre.FullfortAvBruker?.Navn ?? ordre.AnsvarligMontor?.Navn;
        var erUtfort = ordre.Status is ArbeidsordreStatus.Ferdig or ArbeidsordreStatus.Fakturert;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.8f, Unit.Centimetre);
                page.MarginTop(1.6f, Unit.Centimetre);
                page.MarginBottom(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(PdfStil.Ink).LineHeight(1.35f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(v =>
                        {
                            v.Item().Element(e => org.TegnLogo(e, pdfLogo, aksent));
                            v.Item().PaddingTop(14).Text("Arbeidsrapport").FontSize(20).Bold().FontColor(PdfStil.Ink);
                            v.Item().PaddingTop(1).Text(ordre.Tittel).FontSize(10).FontColor(Dempet);
                        });
                        row.ConstantItem(130).AlignRight().Column(h =>
                        {
                            h.Item().AlignRight().Text("RAPPORT").FontSize(7).Bold().FontColor(Svak).LetterSpacing(0.06f);
                            h.Item().AlignRight().Text($"Nr. {ordre.Id}").FontSize(13).Bold();
                            h.Item().AlignRight().Text((utfortDato ?? DateTime.Today).ToString("dd.MM.yyyy")).FontSize(9).FontColor(Dempet);
                            h.Item().PaddingTop(5).AlignRight()
                                .Background(erUtfort ? GronnLys : aksentLys).PaddingVertical(2).PaddingHorizontal(8)
                                .Text(erUtfort ? "✓ Utført" : ordre.Status.Visningsnavn()).FontSize(8).Bold().FontColor(erUtfort ? Gronn : aksent);
                        });
                    });
                    col.Item().PaddingTop(10).LineHorizontal(2.5f).LineColor(aksent);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    // Nøkkelinfo i en rolig boks, tre kolonner.
                    var felter = new (string Label, string? Verdi)[]
                    {
                        ("Kunde", kunde?.Navn), ("Kontaktperson", kontakt), ("Adresse", adresse),
                        ("Prosjekt", ordre.Prosjekt?.Navn), ("Utført", utfortDato?.ToString("dd.MM.yyyy")), ("Montør", utfortAv)
                    }.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();

                    if (felter.Count > 0)
                    {
                        col.Item().Background(aksentLys).Border(0.75f).BorderColor(Linje).Padding(11).Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                            foreach (var (label, verdi) in felter)
                            {
                                table.Cell().PaddingVertical(4).PaddingRight(10).Column(c =>
                                {
                                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).SemiBold().FontColor(Svak).LetterSpacing(0.05f);
                                    c.Item().PaddingTop(1).Text(verdi).FontSize(9.5f).SemiBold();
                                });
                            }
                        });
                    }

                    Seksjon(col, "Oppdraget", aksent, ordre.Beskrivelse);
                    Seksjon(col, "Utført arbeid", aksent, ordre.UtfortArbeid);

                    if (!string.IsNullOrWhiteSpace(ordre.Anbefalinger))
                    {
                        col.Item().Column(c =>
                        {
                            Overskrift(c, "Anbefalinger", aksent);
                            c.Item().PaddingTop(5).Background(GulLys).BorderLeft(3).BorderColor(Gul)
                                .PaddingVertical(8).PaddingHorizontal(11).Text(ordre.Anbefalinger);
                        });
                    }

                    if (ordre.Sjekkpunkter.Count > 0)
                    {
                        col.Item().Column(c =>
                        {
                            Overskrift(c, "Sjekkliste", aksent);
                            c.Item().PaddingTop(5).Column(liste =>
                            {
                                liste.Spacing(4);
                                foreach (var punkt in ordre.Sjekkpunkter.OrderBy(p => p.Rekkefolge))
                                {
                                    liste.Item().Row(r =>
                                    {
                                        r.ConstantItem(17).PaddingTop(1).Element(e =>
                                        {
                                            var boks = e.Width(11).Height(11);
                                            if (punkt.Fullfort)
                                            {
                                                boks.Background(aksent).AlignMiddle().AlignCenter().Text("✓").FontSize(7.5f).Bold().FontColor(Colors.White);
                                            }
                                            else
                                            {
                                                boks.Border(1.2f).BorderColor(aksent);
                                            }
                                        });
                                        r.RelativeItem().Text(punkt.Tekst).FontColor(punkt.Fullfort ? PdfStil.Ink : Dempet);
                                    });
                                }
                            });
                        });
                    }

                    if (materiell.Count > 0)
                    {
                        col.Item().Column(c =>
                        {
                            Overskrift(c, "Materiell brukt", aksent);
                            c.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(70); });
                                table.Header(h =>
                                {
                                    h.Cell().BorderBottom(1.2f).BorderColor(Linje).PaddingVertical(4).Text("MATERIELL").FontSize(7).SemiBold().FontColor(Svak);
                                    h.Cell().BorderBottom(1.2f).BorderColor(Linje).PaddingVertical(4).AlignRight().Text("ANTALL").FontSize(7).SemiBold().FontColor(Svak);
                                });
                                foreach (var (navn, antall) in materiell)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).Text(navn);
                                    table.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).AlignRight().Text(antall);
                                }
                            });
                        });
                    }

                    if (ordre.Media.Count > 0)
                    {
                        col.Item().Column(c =>
                        {
                            Overskrift(c, "Bilder", aksent);
                            foreach (var rad in ordre.Media.Chunk(3))
                            {
                                c.Item().PaddingTop(6).Row(r =>
                                {
                                    r.Spacing(8);
                                    foreach (var bilde in rad)
                                    {
                                        r.RelativeItem().Height(130).Image(PdfBilde.Forminsk(bilde.Data)).FitArea();
                                    }
                                    for (var i = rad.Length; i < 3; i++)
                                    {
                                        r.RelativeItem();
                                    }
                                });
                            }
                        });
                    }

                    // Signaturlinjer - montør og dato fylles ut, kunden kvitterer for hånd.
                    var utfortTekst = string.Join(", ", new[]
                    {
                        string.IsNullOrWhiteSpace(utfortAv) ? "Utført av" : $"Utført av {utfortAv}",
                        utfortDato?.ToString("dd.MM.yyyy")
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    col.Item().PaddingTop(18).ShowEntire().Row(r =>
                    {
                        r.Spacing(30);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Height(26);
                            c.Item().LineHorizontal(0.75f).LineColor(Svak);
                            c.Item().PaddingTop(3).Text(utfortTekst).FontSize(8.5f).FontColor(Dempet);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Height(26);
                            c.Item().LineHorizontal(0.75f).LineColor(Svak);
                            c.Item().PaddingTop(3).Text($"Kvittert av kunde{(string.IsNullOrWhiteSpace(kontakt) ? "" : $" · {kontakt}")}").FontSize(8.5f).FontColor(Dempet);
                        });
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(0.75f).BorderColor(Linje).PaddingTop(6).Row(row =>
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
            });
        });

        return document.GeneratePdf();
    }

    // Arbeid (montasje, timer, kjøring) er ikke materiell - og timer skal ikke
    // stå i rapporten.
    private static readonly string[] ArbeidsEnheter = ["t", "time", "timer", "rs", "km"];
    private static readonly string[] ArbeidsOrd = ["montasje", "montering", "arbeid", "kjøring", "reise", "time"];

    private static bool ErArbeidslinje(TilbudLinje l) =>
        l.ComponentId is null
        && (ArbeidsEnheter.Contains((l.Enhet ?? "").Trim().ToLowerInvariant())
            || ArbeidsOrd.Any(o => l.Navn.Contains(o, StringComparison.OrdinalIgnoreCase)));

    private static void Overskrift(ColumnDescriptor col, string tittel, Color aksent) =>
        col.Item().Text(tittel.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(aksent).LetterSpacing(0.09f);

    private static void Seksjon(ColumnDescriptor col, string tittel, Color aksent, string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return;
        }

        col.Item().Column(c =>
        {
            Overskrift(c, tittel, aksent);
            c.Item().PaddingTop(4).Text(tekst);
        });
    }

    // Avsenderen på rapporten: organisasjonen som er logget inn. Standard-
    // organisasjonen (og manglende felt) faller tilbake til FirmaInfo og
    // itlock-logoen, slik rapportene så ut før.
    private sealed record Organisasjon(string Navn, string Bunntekst, string Farge, string FargeLys, Tenant? Tenant)
    {
        public static Organisasjon Fra(Tenant? t)
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

            return new Organisasjon(navn, bunntekst, tema.Primaer, tema.PrimaerLys, t);
        }

        public void TegnLogo(IContainer container, PdfLogo standardLogo, Color aksent)
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
            container.Text(Navn).FontSize(14).Bold().FontColor(aksent);
        }
    }
}
