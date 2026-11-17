using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Ordrebekreftelse som lages automatisk når et tilbud blir godkjent. Lagres på
// tilbudet (LagretPdf, EntityType «Ordrebekreftelse») - sendes ikke på e-post.
// Oppsett som en vanlig ordrebekreftelse (logo, ordrenr/dato, kunde og
// leveringsadresse, referanser, varelinjer, sum) + levering/oppmøte,
// salgspant og avtalebetingelser. PDF-stil N1: hvit topp og tynne linjer.
public class OrdrebekreftelsePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public const string EntityType = "Ordrebekreftelse";
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");
    private static readonly Color Ink = Color.FromHex("#292927");
    private static readonly Color Svak = Color.FromHex("#6D675F");
    private static readonly Color Linje = Color.FromHex("#D9D6D0");
    private const decimal MvaSats = 0.25m;

    public sealed record Resultat(byte[] Pdf, string Ordrenummer, int Versjon);

    public async Task<Resultat?> LagAsync(int tilbudId, int? brukerId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Prosjektleder)
            .Include(t => t.Linjer).ThenInclude(l => l.Component)
            .Include(t => t.KodeLinjer)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tilbudId);
        if (tilbud is null)
        {
            return null;
        }

        var tidligere = await db.LagredePdfer.CountAsync(p => p.EntityType == EntityType && p.EntityId == tilbudId);
        var versjon = tidligere + 1;
        var ordrenummer = $"{tilbud.OpprettetDato.Year}-{tilbud.Id:000}" + (versjon > 1 ? $"-{versjon}" : "");
        var bruker = brukerId is int bid ? await db.Brukere.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bid) : null;

        var org = RapportProfil.Fra(tenantContext.Current);
        var prosjekt = tilbud.Prosjekt;
        var kunde = prosjekt?.Kunde;
        var erPrivat = kunde?.Type == KundeType.Privatperson;
        var varer = tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering && l.Antall != 0)
            .OrderBy(l => l.Rekkefolge).ToList();
        var sumVarer = varer.Sum(l => l.Utpris * l.Antall);
        var totaltEks = TilbudBeregningHelper.BeregnTotaltUtenMva(tilbud);
        var arbeid = new List<(string Kode, string Navn, decimal Belop)>();
        if (tilbud.BrukerKodelinjer && tilbud.KodeLinjer.Count > 0)
        {
            arbeid = tilbud.KodeLinjer.OrderBy(l => l.Rekkefolge).ThenBy(l => l.Id)
                .Select(l => (l.Kode, l.Navn, KodeLinjeBeregner.Sum(l, tilbud.KodeLinjer, sumVarer).Salg))
                .Where(x => x.Item3 != 0).ToList();
        }
        else if (totaltEks - sumVarer != 0)
        {
            arbeid.Add(("", "Montering og arbeid", totaltEks - sumVarer));
        }
        var mva = Math.Round(totaltEks * MvaSats, 2);

        string? Adr(string? a, string? postnr, string? sted)
        {
            var linje2 = string.Join(" ", new[] { postnr, sted }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var deler = new[] { a, linje2 }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return deler.Count == 0 ? null : string.Join("\n", deler);
        }
        var kundeAdresse = kunde is null ? null : Adr(kunde.Adresse, kunde.Postnr, kunde.Sted);
        var leveringsadresse = prosjekt is null ? null : Adr(prosjekt.Adresse, prosjekt.Postnr, prosjekt.Sted);
        var varReferanse = prosjekt?.Prosjektleder?.Navn ?? bruker?.Navn ?? tilbud.OpprettetAvNavn;
        var kontaktEpost = prosjekt?.Prosjektleder?.Epost ?? org.Epost;

        var harRabatt = varer.Any(l => l.RabattProsent > 0 && l.PrisVeiledende is > 0);
        var leveringsmate = arbeid.Count > 0 ? "Levering og montering" : "Levering";
        var strekkode = Strekkode.Svg(null, ordrenummer);
        var kundeLinjer = new List<string?> { kunde?.Navn, kunde?.Adresse, string.Join(" ", new[] { kunde?.Postnr, kunde?.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))), "Norge" }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
        var leveringLinjer = new List<string?> { kunde?.Navn, tilbud.KontaktpersonNavn ?? kunde?.Kontaktperson,
                prosjekt?.Adresse ?? kunde?.Adresse,
                string.Join(" ", new[] { prosjekt?.Postnr ?? kunde?.Postnr, (prosjekt?.Sted ?? kunde?.Sted)?.ToUpperInvariant() }.Where(s => !string.IsNullOrWhiteSpace(s))), "Norge" }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();

        void Stiplet(IContainer c) => c.LineHorizontal(1).LineColor(Ink).LineDashPattern([5f, 4f]);

        var pdf = Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(1.6f, Unit.Centimetre);
            page.MarginVertical(1.2f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

            // Bunn som i malen: stiplet linje, firma / kontakt / web, og leveringsvilkår
            page.Footer().Column(f =>
            {
                f.Item().PaddingBottom(8).Element(Stiplet);
                f.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text(org.Navn);
                        foreach (var del in (org.Adresse ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            c.Item().Text(del);
                        }
                    });
                    r.RelativeItem().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.ConstantColumn(55); cd.RelativeColumn(); });
                        void R(string a, string? b) { if (string.IsNullOrWhiteSpace(b)) return; t.Cell().Text(a); t.Cell().Text(b); }
                        R("Tlf.", org.Telefon);
                        R("Org.nr", org.OrgNr);
                        R("E-post", org.Epost);
                    });
                });
                f.Item().PaddingTop(8).Text("Ordren leveres i henhold til betingelsene i denne ordrebekreftelsen.").FontSize(8);
            });

            page.Header().AlignRight().Text(t =>
            {
                t.Span("Side ");
                t.CurrentPageNumber();
                t.Span("/");
                t.TotalPages();
            });

            page.Content().Column(col =>
            {
                // Logo | ORDREBEKREFTELSE + ordrenr/dato | strekkode
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.ConstantItem(150).PaddingTop(4).Element(e => org.TegnLogo(e, pdfLogo, 34));
                    row.RelativeItem().PaddingLeft(30).Column(c =>
                    {
                        c.Item().Text("ORDREBEKREFTELSE").FontSize(17).Bold();
                        c.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.ConstantColumn(110); cd.RelativeColumn(); });
                            void R(string a, string b) { t.Cell().Text(a); t.Cell().Text(b); }
                            R("Ordrenummer", ordrenummer);
                            R("Dato", DateTime.Today.ToString("dd.MM.yyyy"));
                            if (versjon > 1)
                            {
                                R("Erstatter", $"{tilbud.OpprettetDato.Year}-{tilbud.Id:000}{(versjon > 2 ? $"-{versjon - 1}" : "")}");
                            }
                        });
                    });
                    if (strekkode is not null)
                    {
                        row.ConstantItem(150).Height(46).AlignRight().Svg(strekkode).FitArea();
                    }
                });

                // Kunde og leveringsadresse til venstre, opplysninger til høyre
                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        foreach (var l in kundeLinjer) c.Item().Text(l);
                        c.Item().PaddingTop(16).Text("Leveringsadresse").Bold();
                        foreach (var l in leveringLinjer) c.Item().Text(l);
                    });
                    row.RelativeItem(1.25f).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.ConstantColumn(150); cd.RelativeColumn(); });
                        void R(string a, string? b) { t.Cell().PaddingBottom(1).Text(a); t.Cell().PaddingBottom(1).Text(b ?? ""); }
                        R("Kundenummer", kunde?.TripletexKundenummer ?? kunde?.Id.ToString());
                        R("Deres referanse", tilbud.KontaktpersonNavn ?? kunde?.Kontaktperson);
                        R("Vår referanse", varReferanse);
                        R("Leveringsmåte", leveringsmate);
                        R("Leveringstid", string.IsNullOrWhiteSpace(tilbud.Leveringstid) ? "Avtales nærmere" : tilbud.Leveringstid);
                        R("Betalingsbetingelser", "Iht. faktura");
                        R("Tilbud nr.", $"T-{tilbud.OpprettetDato.Year}-{tilbud.Id:000}");
                        R("Godkjent dato", (tilbud.GodkjentDato ?? DateTime.Today).ToString("dd.MM.yyyy"));
                        R("Prosjekt ID", prosjekt is null ? null : $"P-{prosjekt.Id}");
                        R("Prosjektnavn", prosjekt?.Navn);
                    });
                });

                // Varelinjer mellom stiplede linjer
                col.Item().PaddingTop(40).Element(Stiplet);
                col.Item().PaddingTop(2).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(95);
                        cd.RelativeColumn(3);
                        cd.ConstantColumn(55);
                        cd.ConstantColumn(80);
                        cd.ConstantColumn(50);
                        cd.ConstantColumn(80);
                    });
                    t.Header(h =>
                    {
                        IContainer H(IContainer c) => c.BorderBottom(0.75f).BorderColor(Ink).PaddingVertical(5);
                        h.Cell().Element(H).Text("Nummer").Bold();
                        h.Cell().Element(H).Text("Beskrivelse").Bold();
                        h.Cell().Element(H).AlignRight().Text("Antall").Bold();
                        h.Cell().Element(H).AlignRight().Text("Pris pr enhet").Bold();
                        h.Cell().Element(H).AlignRight().Text("Rabatt %").Bold();
                        h.Cell().Element(H).AlignRight().Text("Totalt").Bold();
                    });
                    IContainer C(IContainer c) => c.PaddingVertical(5);
                    foreach (var l in varer)
                    {
                        var medRabatt = harRabatt && l.RabattProsent > 0 && l.PrisVeiledende is > 0;
                        t.Cell().Element(C).Text(l.Component?.Produktkode ?? "");
                        t.Cell().Element(C).Column(c =>
                        {
                            c.Item().Text(l.Navn);
                            var under = string.Join(" · ", new[] { l.Component?.Overflate, l.Component?.Leverandor }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (under.Length > 0) c.Item().Text(under).FontSize(8);
                        });
                        t.Cell().Element(C).AlignRight().Text($"{l.Antall}{(string.IsNullOrWhiteSpace(l.Enhet) || l.Enhet.Equals("stk", StringComparison.OrdinalIgnoreCase) ? "" : " " + l.Enhet)}");
                        t.Cell().Element(C).AlignRight().Text(Kr(medRabatt ? l.PrisVeiledende!.Value : l.Utpris));
                        t.Cell().Element(C).AlignRight().Text(medRabatt ? l.RabattProsent.ToString("0.#", Kultur) : "");
                        t.Cell().Element(C).AlignRight().Text(Kr(l.Utpris * l.Antall));
                    }
                    foreach (var (kode, navn, belop) in arbeid)
                    {
                        t.Cell().Element(C).Text(kode);
                        t.Cell().Element(C).Text(navn);
                        t.Cell().Element(C).AlignRight().Text("1");
                        t.Cell().Element(C).AlignRight().Text(Kr(belop));
                        t.Cell().Element(C).AlignRight().Text("");
                        t.Cell().Element(C).AlignRight().Text(Kr(belop));
                    }
                });

                // Sum som i malen: kort strek over beløpet, «Totalt NOK»
                col.Item().PaddingTop(14).AlignRight().Width(250).Table(t =>
                {
                    t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(90); });
                    t.Cell().PaddingVertical(1).AlignRight().PaddingRight(12).Text("Sum eks. mva");
                    t.Cell().PaddingVertical(1).AlignRight().Text(Kr(totaltEks));
                    t.Cell().PaddingVertical(1).AlignRight().PaddingRight(12).Text("Mva 25 %");
                    t.Cell().PaddingVertical(1).AlignRight().Text(Kr(mva));
                    t.Cell();
                    t.Cell().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                    t.Cell().PaddingTop(4).AlignRight().PaddingRight(12).Text("Totalt NOK").Bold();
                    t.Cell().PaddingTop(4).AlignRight().Text(Kr(totaltEks + mva)).Bold();
                });

                col.Item().PaddingTop(24).Text("Takk for bestillingen! Tidspunkt for levering og montering avtales nærmere – montøren ringer før ankomst. Sørg for at det er ryddet og fri tilgang til døren/området.");

                // Betingelser
                col.Item().PaddingTop(16).Text("Betingelser").Bold();
                Punkt(col, $"Salgspant og eiendomsrett: {org.Navn} forbeholder seg salgspant i de leverte varene som sikkerhet for kjøpesummen, inkludert renter og omkostninger, i medhold av panteloven §§ 3-14 til 3-22. Eiendomsretten går over til kjøper først når hele kjøpesummen er betalt.");
                Punkt(col, "Spesialtilpassede varer: Det gis ikke retur- eller avbestillingsrett på spesialtilpassede systemlåser, lisensierte nøkler eller prosjektbestilte låsvarer etter at ordren er satt i produksjon eller bekreftet bestilt hos produsent.");
                Punkt(col, "Avbestilling av tjenester: Avbestilling eller endring av avtalt montering må skje senest 24 timer før oppmøte. Ved for sen avbestilling eller manglende tilgang påløper bomtur etter gjeldende satser.");
                if (erPrivat)
                {
                    Punkt(col, "Angrerett (forbrukerkjøp): 14 dagers angrerett på standard lagervarer i ubrukt stand og originalemballasje. Angreretten bortfaller for tjenester (som montering) når arbeidet er påbegynt eller fullført etter avtale.");
                }
                var kontakt = string.Join(" · ", new[] { varReferanse, org.Telefon, kontaktEpost }.Where(s => !string.IsNullOrWhiteSpace(s)));
                col.Item().PaddingTop(12).Text($"Spørsmål eller feil i ordren? Ta kontakt{(kontakt.Length > 0 ? ": " + kontakt : ".")}");
            });
        })).GeneratePdf();

        return new Resultat(pdf, ordrenummer, versjon);
    }

    private static void Seksjon(ColumnDescriptor col, string tittel) =>
        col.Item().PaddingTop(16).BorderBottom(0.5f).BorderColor(Linje).PaddingBottom(3).Text(tittel).FontSize(10.5f).Bold();

    private static void Punkt(ColumnDescriptor col, string tekst) =>
        col.Item().PaddingTop(4).Row(r =>
        {
            r.ConstantItem(10).Text("•");
            r.RelativeItem().Text(tekst).FontSize(9);
        });

    private static string Kr(decimal v) => v.ToString("N2", Kultur) + " kr";
}
