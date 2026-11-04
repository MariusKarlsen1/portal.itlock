using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Arbeidsrapport til kunden (E10, sluttrapport-PDF-forslag): grå topp helt ut med
// tittel, logo og fakta, så Hva er gjort, Utbedringer, Sjekkliste og Bilder med
// tynn linje mellom delene, og signaturlinjer. Timer, kjøring, materiell og
// priser er ikke med. Sjekkliste og bilder kan velges bort; tomme deler utelates.
public class ArbeidsordrePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private const float Marg = 51;
    private static readonly Color Gronn = Color.FromHex("#2E7D4F");
    private static readonly Color Brun = Color.FromHex("#A0621A");
    private static readonly Color Strek = Color.FromHex("#9AA3B5");

    public async Task<byte[]?> GenerateAsync(int arbeidsordreId, bool medSjekkliste = true, bool medBilder = true)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.AnsvarligMontor)
            .Include(a => a.FullfortAvBruker)
            .Include(a => a.Sjekkpunkter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId);

        if (ordre is null)
        {
            return null;
        }

        var bilder = medBilder
            ? await db.ArbeidsordreMedia
                .Where(m => m.ArbeidsordreId == arbeidsordreId && m.ContentType.StartsWith("image/"))
                .OrderBy(m => m.OpprettetDato)
                .Select(m => m.Data)
                .ToListAsync()
            : [];
        var punkter = medSjekkliste ? ordre.Sjekkpunkter.OrderBy(p => p.Rekkefolge).ToList() : [];

        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = ordre.Kunde ?? ordre.Prosjekt?.Kunde;
        // Kontakt valgt på ordren går foran prosjektets («-» = ingen).
        var kontakt = ordre.Kontaktperson switch
        {
            "-" => null,
            { } k when !string.IsNullOrWhiteSpace(k) => k,
            _ => !string.IsNullOrWhiteSpace(ordre.Prosjekt?.Kontaktperson) ? ordre.Prosjekt!.Kontaktperson : kunde?.Kontaktperson
        };
        var adresse = string.Join(", ", new[] { ordre.Adresse, ordre.Sted }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(adresse))
        {
            adresse = ordre.Prosjekt?.Adresse ?? kunde?.Adresse ?? "";
        }

        var utfortDato = ordre.FullfortDato ?? ordre.PlanlagtDato;
        var utfortAv = ordre.FullfortAvBruker?.Navn ?? ordre.AnsvarligMontor?.Navn;
        var fakta = new (string Etikett, string? Verdi)[]
        {
            ("Kunde", kunde?.Navn), ("Adresse", adresse), ("Utført", utfortDato?.ToString("dd.MM.yyyy")), ("Arbeidsordre", $"Nr. {ordre.Id}")
        }.Where(f => !string.IsNullOrWhiteSpace(f.Verdi)).ToList();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(0);
                page.MarginTop(0);
                page.MarginBottom(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(ListePdfStil.Ink).LineHeight(1.4f));

                // Grå topp helt ut på første side; bare luft i toppen på de neste.
                page.Header().Column(h =>
                {
                    h.Item().ShowOnce().Background(ListePdfStil.Stripe).PaddingHorizontal(Marg).PaddingTop(40).PaddingBottom(16).Column(t =>
                    {
                        t.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Arbeidsrapport").FontSize(20).ExtraBold().LineHeight(1.15f);
                                if (!string.IsNullOrWhiteSpace(ordre.Tittel))
                                {
                                    c.Item().PaddingTop(1).Text(ordre.Tittel).FontSize(10).FontColor(ListePdfStil.Mut);
                                }
                            });
                            row.ConstantItem(150).AlignRight().Element(e => org.TegnLogo(e, pdfLogo, 26));
                        });
                        if (fakta.Count > 0)
                        {
                            t.Item().PaddingTop(14).Row(row =>
                            {
                                foreach (var (etikett, verdi) in fakta)
                                {
                                    row.RelativeItem().PaddingRight(8).Column(c =>
                                    {
                                        c.Item().Text(etikett.ToUpperInvariant()).FontSize(6.5f).Bold().FontColor(ListePdfStil.Mut).LetterSpacing(0.07f);
                                        c.Item().PaddingTop(1).Text(verdi).FontSize(9.5f).Bold();
                                    });
                                }
                            });
                        }
                    });
                    h.Item().SkipOnce().Height(40);
                });

                page.Content().PaddingHorizontal(Marg).PaddingTop(4).Column(col =>
                {
                    // Hver del med overskrift; tynn linje over alle unntatt den første.
                    var forste = true;
                    void Del(string tittel, string? antall, Action<ColumnDescriptor> innhold)
                    {
                        var del = forste
                            ? col.Item().PaddingTop(14)
                            : col.Item().PaddingTop(14).BorderTop(0.6f).BorderColor(ListePdfStil.Ramme).PaddingTop(12);
                        forste = false;
                        del.Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span(tittel).FontSize(11).Bold();
                                if (antall is not null)
                                {
                                    t.Span("   " + antall).FontSize(9).FontColor(ListePdfStil.Mut);
                                }
                            });
                            c.Item().PaddingTop(5).Column(innhold);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(ordre.UtfortArbeid))
                    {
                        Del("Hva er gjort", null, c => RapportMal.Tekst(c.Item(), ordre.UtfortArbeid!));
                    }

                    if (!string.IsNullOrWhiteSpace(ordre.Anbefalinger))
                    {
                        Del("Utbedringer", null, c => RapportMal.Tekst(c.Item(), ordre.Anbefalinger!));
                    }

                    if (punkter.Count > 0)
                    {
                        Del("Sjekkliste", $"{punkter.Count} {(punkter.Count == 1 ? "punkt" : "punkter")}", c =>
                        {
                            for (var i = 0; i < punkter.Count; i++)
                            {
                                var p = punkter[i];
                                var (tekst, farge) = p.Resultat switch
                                {
                                    SjekkpunktResultat.Ok => ("OK", Gronn),
                                    SjekkpunktResultat.Avvik => ("Avvik", Brun),
                                    SjekkpunktResultat.IkkeAktuelt => ("Ikke aktuelt", ListePdfStil.Mut),
                                    _ => (p.Fullfort ? "OK" : "–", p.Fullfort ? Gronn : ListePdfStil.Mut)
                                };
                                var rad = i < punkter.Count - 1 ? c.Item().BorderBottom(0.5f).BorderColor(ListePdfStil.Linje) : c.Item();
                                rad.PaddingVertical(4).Row(r =>
                                {
                                    r.ConstantItem(62).Text(tekst).Bold().FontColor(farge);
                                    r.RelativeItem().Text(p.Tekst);
                                    if (!string.IsNullOrWhiteSpace(p.Kommentar))
                                    {
                                        r.RelativeItem().AlignRight().Text(p.Kommentar).FontColor(ListePdfStil.Mut);
                                    }
                                });
                            }
                        });
                    }

                    if (bilder.Count > 0)
                    {
                        Del("Bilder", bilder.Count.ToString(), c =>
                        {
                            foreach (var rad in bilder.Chunk(3))
                            {
                                c.Item().PaddingBottom(8).Row(r =>
                                {
                                    r.Spacing(8);
                                    foreach (var bilde in rad)
                                    {
                                        r.RelativeItem().Height(120).Image(PdfBilde.Forminsk(bilde)).FitArea();
                                    }
                                    for (var i = rad.Length; i < 3; i++)
                                    {
                                        r.RelativeItem();
                                    }
                                });
                            }
                        });
                    }

                    // Signaturlinjer: den som utførte jobben og kunden.
                    col.Item().PaddingTop(28).ShowEntire().Row(r =>
                    {
                        r.Spacing(30);
                        foreach (var tekst in new[]
                        {
                            string.Join(" · ", new[] { org.Navn, utfortAv, utfortDato?.ToString("dd.MM.yyyy") }.Where(x => !string.IsNullOrWhiteSpace(x))),
                            string.IsNullOrWhiteSpace(kontakt) ? "Kunde" : $"Kunde · {kontakt}"
                        })
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Height(22);
                                c.Item().LineHorizontal(0.75f).LineColor(Strek);
                                c.Item().PaddingTop(3).Text(tekst).FontSize(8).FontColor(ListePdfStil.Mut);
                            });
                        }
                    });
                });

                page.Footer().PaddingHorizontal(Marg).PaddingTop(8).BorderTop(0.6f).BorderColor(ListePdfStil.Ramme).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(org.Bunntekst).FontSize(7).FontColor(ListePdfStil.Mut);
                    row.AutoItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(7).FontColor(ListePdfStil.Mut));
                        t.Span("Side ");
                        t.CurrentPageNumber();
                        t.Span(" av ");
                        t.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
