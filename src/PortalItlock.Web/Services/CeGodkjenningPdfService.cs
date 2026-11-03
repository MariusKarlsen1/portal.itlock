using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// CE-sertifikatet som PDF, tre deler:
//   1. Sertifikatet - én side med resultatet, døren, automatikken, nøkkelmålingene,
//      signaturene og QR-kode til døren. Kan gis til kunden alene.
//   2. Kontrollprotokollen - alle krav i én tabell, tilbehøret og bildene.
//   3. EU-samsvarserklæringen - produkt, installatør, direktiver og standarder.
// Firmanavn, logo og kontaktinfo hentes fra organisasjonen (RapportProfil), så
// PDF-en passer for alle bedrifter som bruker portalen.
public class CeGodkjenningPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    private static readonly Color Ink = Color.FromHex("#14213D");
    private static readonly Color Dempet = Color.FromHex("#5B6680");
    private static readonly Color Linje = Color.FromHex("#E3E7ED");
    private static readonly Color LinjeLys = Color.FromHex("#EDF0F4");
    private static readonly Color Hode = Color.FromHex("#F5F6F8");
    private static readonly Color Annenhver = Color.FromHex("#F8F9FB");
    private static readonly Color Gronn = Color.FromHex("#2B7A4B");
    private static readonly Color GronnLys = Color.FromHex("#EEF8F1");
    private static readonly Color GronnKant = Color.FromHex("#C6E6D1");
    private static readonly Color Rod = Color.FromHex("#A32020");
    private static readonly Color RodLys = Color.FromHex("#FDF0F0");
    private static readonly Color RodKant = Color.FromHex("#F2C6C6");
    private static readonly Color Gul = Color.FromHex("#8A5A00");
    private static readonly Color GulLys = Color.FromHex("#FDF6E7");
    private static readonly Color Gra = Color.FromHex("#545C65");
    private static readonly Color GraLys = Color.FromHex("#F2F4F7");

    private enum KravStatus { Oppfylt, IkkeOppfylt, IkkeSvart, Registrert, Unntatt }

    private sealed record Krav(string Tekst, string? Resultat, string? Grense, KravStatus Status);

    // baseUrl brukes til QR-koden som peker på døren (/dor/{id}). Uten den lages PDF-en uten QR.
    public async Task<byte[]?> GenerateAsync(int ceGodkjenningId, string? baseUrl = null)
    {
        var ce = await db.CeGodkjenninger
            .Include(c => c.Dor).ThenInclude(d => d!.Prosjekt)
            .Include(c => c.Media)
            .FirstOrDefaultAsync(c => c.Id == ceGodkjenningId);

        if (ce is null)
        {
            return null;
        }

        var dorKomponenter = await db.DorKomponenter
            .Where(k => k.DorId == ce.DorId)
            .Include(k => k.Component).ThenInclude(c => c!.Type)
            .ToListAsync();
        var grenser = await db.CeMaleGrenseverdier.FirstOrDefaultAsync() ?? new CeMaleGrenseverdier();

        var tilbehor = dorKomponenter
            .Where(k => k.Component?.Type?.CeKategori is CeTilbehorKategori.SikkerhetsTilbehor or CeTilbehorKategori.AnnetTilbehor)
            .ToList();
        var validering = CeValideringsService.Evaluer(ce, dorKomponenter);
        var godkjent = validering.All(v => v.Bestatt);
        var ikkeOppfylt = validering.Count(v => !v.Bestatt);
        var org = RapportProfil.Fra(tenantContext.Current);
        var qr = string.IsNullOrWhiteSpace(baseUrl) ? null : QrCodeService.GeneratePng($"{baseUrl.TrimEnd('/')}/dor/{ce.DorId}");
        var dornummer = ce.Dornummer ?? ce.Dor?.Dornummer ?? "-";
        var kontrollert = ce.UtfortAvDato ?? ce.OpprettetDato;

        var dor = KravDoren(ce);
        var mal = KravMal(ce, grenser);
        var sikkerhet = KravSikkerhet(ce);

        var document = Document.Create(doc =>
        {
            // ---------- Side 1: sertifikatet ----------
            doc.Page(page =>
            {
                Oppsett(page);
                page.Header().Row(row =>
                {
                    row.RelativeItem().Height(30).AlignMiddle().Element(e => org.TegnLogo(e, pdfLogo, 26));
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text(org.Navn).FontSize(7.5f).SemiBold().FontColor(Dempet);
                        foreach (var linje in new[] { org.OrgNr is null ? null : $"Org.nr {org.OrgNr}", org.Adresse, string.Join(" · ", new[] { org.Telefon, org.Epost }.Where(s => !string.IsNullOrWhiteSpace(s))) })
                        {
                            if (!string.IsNullOrWhiteSpace(linje))
                            {
                                c.Item().AlignRight().Text(linje).FontSize(7.5f).FontColor(Dempet);
                            }
                        }
                    });
                });

                page.Content().PaddingTop(22).Column(col =>
                {
                    col.Item().Text("SAMSVARSSERTIFIKAT · CE").FontSize(7.5f).Bold().LetterSpacing(0.12f).FontColor(org.Aksent);
                    col.Item().PaddingTop(3).Text($"Dørautomatikk {dornummer}").FontSize(22).Bold();
                    col.Item().PaddingTop(1).Text(string.Join(" · ", new[] { ce.DorTil, ce.ProsjektNavn, ce.Adresse }.Where(s => !string.IsNullOrWhiteSpace(s))))
                        .FontSize(9).FontColor(Dempet);

                    // Resultatet
                    col.Item().PaddingTop(16).Border(0.75f).BorderColor(godkjent ? GronnKant : RodKant).CornerRadius(10).Row(row =>
                    {
                        row.RelativeItem().Background(godkjent ? GronnLys : RodLys).Padding(14).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.ConstantItem(18).Height(18).AlignMiddle().Svg(godkjent ? SvgHake("#2B7A4B") : SvgKryss("#A32020")).FitArea();
                                r.ConstantItem(6);
                                r.RelativeItem().AlignMiddle().Text(godkjent
                                        ? $"Godkjent – alle {validering.Count} krav er oppfylt"
                                        : $"Ikke godkjent – {ikkeOppfylt} av {validering.Count} krav er ikke oppfylt")
                                    .FontSize(13.5f).Bold().FontColor(godkjent ? Gronn : Rod);
                            });
                            c.Item().PaddingTop(3).Text(godkjent
                                    ? "Kontrollert etter NS-EN 16005. Dørautomatikken kan tas i bruk."
                                    : "Kontrollert etter NS-EN 16005. Kravene som ikke er oppfylt står i kontrollprotokollen.")
                                .FontSize(8.5f).FontColor(godkjent ? Color.FromHex("#2F5F43") : Rod);
                        });
                        if (qr is not null)
                        {
                            row.ConstantItem(150).BorderLeft(0.75f).BorderColor(godkjent ? GronnKant : RodKant).Padding(10).Row(r =>
                            {
                                r.ConstantItem(56).Height(56).Image(qr).FitArea();
                                r.ConstantItem(8);
                                r.RelativeItem().AlignMiddle().Text("Skann for dørens historikk og service").FontSize(7).FontColor(Dempet);
                            });
                        }
                    });

                    // Fakta
                    col.Item().PaddingTop(12).Border(0.75f).BorderColor(Linje).CornerRadius(8).Row(row =>
                    {
                        Fakta(row, "Sertifikatnr.", ce.Sertifiseringsnummer, forste: true);
                        Fakta(row, "Kontrollert", kontrollert.ToString("dd.MM.yyyy"));
                        Fakta(row, "Gyldig til", ce.GyldigTil.ToString("dd.MM.yyyy"));
                        Fakta(row, "Status", ce.Status.Visningsnavn());
                    });

                    // Døren og automatikken
                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Element(e => Boks(e, "Døren", c => Liste(c,
                        [
                            ("Dørnummer", string.Join(" · ", new[] { dornummer, ce.DorTil }.Where(s => !string.IsNullOrWhiteSpace(s)))),
                            ("Dør-ID", ce.DorIdKode),
                            ("Konstruksjon", string.Join(" / ", new[] { ce.Dorkonstruksjon, ce.Karmkonstruksjon }.Where(s => !string.IsNullOrWhiteSpace(s)))),
                            ("Mål (B × H)", ce.BreddeMm is null && ce.HoydeMm is null ? null : $"{ce.BreddeMm?.ToString() ?? "-"} × {ce.HoydeMm?.ToString() ?? "-"} mm"),
                            ("Etasje / bygg", string.Join(" · ", new[] { ce.Etasje, ce.Bygg }.Where(s => !string.IsNullOrWhiteSpace(s)))),
                            ("Brannklasse", ce.Brannklasse)
                        ])));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(e => Boks(e, "Automatikken", c => Liste(c,
                        [
                            ("Produsent", ce.Produsent),
                            ("Modell", ce.ItemNavn),
                            ("Serienummer", ce.Serienummer),
                            ("Produksjonsår", ce.ProduksjonsAar?.ToString()),
                            ("Kunde", ce.KundeNavn),
                            ("Kontakt", ce.Kontaktperson)
                        ])));
                    });

                    // Nøkkelmålinger
                    col.Item().PaddingTop(14).Text("NØKKELMÅLINGER").FontSize(8.5f).Bold().LetterSpacing(0.06f);
                    col.Item().PaddingTop(5).Element(e => KravTabell(e, [("", mal.Take(4).ToList())], medGruppe: false));

                    // Signaturer
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Element(e => Signatur(e, ce.UtfortAvSignatur, ce.UtfortAvNavn, $"Utført {ce.UtfortAvDato?.ToString("dd.MM.yyyy") ?? ""}"));
                        row.ConstantItem(24);
                        row.RelativeItem().Element(e => Signatur(e, ce.VerifisertAvSignatur, ce.VerifisertAvNavn, $"Kontrollert {ce.VerifisertAvDato?.ToString("dd.MM.yyyy") ?? ""}"));
                    });
                });

                Bunn(page, ce, dornummer, org);
            });

            // ---------- Side 2: kontrollprotokollen ----------
            doc.Page(page =>
            {
                Oppsett(page);
                SideHode(page, org, "Kontrollprotokoll", $"{dornummer} · {ce.Sertifiseringsnummer}");
                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Element(e => KravTabell(e, [("Døren", dor), ("Mål og tider", mal), ("Sikkerhet", sikkerhet)], medGruppe: true));

                    var kommentarer = new[] { ("Kommentar til målingene", ce.MalKommentar), ("Andre funksjoner testet", ce.EkstraFunksjonerKommentar) }
                        .Where(k => !string.IsNullOrWhiteSpace(k.Item2)).ToList();
                    foreach (var (tittel, tekst) in kommentarer)
                    {
                        col.Item().PaddingTop(8).Text(t =>
                        {
                            t.Span(tittel + ": ").SemiBold();
                            t.Span(tekst!);
                        });
                    }

                    // Tilbehøret og bildene deles ikke over to sider.
                    if (tilbehor.Count > 0)
                    {
                        col.Item().PaddingTop(14).ShowEntire().Column(t =>
                        {
                            t.Item().Text("TILBEHØR PÅ DØREN").FontSize(8.5f).Bold().LetterSpacing(0.06f);
                            t.Item().PaddingTop(5).Element(e => TilbehorTabell(e, tilbehor));
                        });
                    }

                    var bilder = ce.Media
                        .Where(m => m.ContentType is "image/jpeg" or "image/jpg" or "image/png")
                        .Take(8)
                        .ToList();
                    col.Item().PaddingTop(14).ShowEntire().Column(bildeKol =>
                    {
                    bildeKol.Item().Text("BILDER").FontSize(8.5f).Bold().LetterSpacing(0.06f);
                    if (ce.FotograferingIkkeTillatt)
                    {
                        bildeKol.Item().PaddingTop(4).Text("Fotografering ikke tillatt – sikret område.").FontColor(Dempet);
                    }
                    else if (bilder.Count == 0)
                    {
                        bildeKol.Item().PaddingTop(4).Text("Ingen bilder lagt ved.").FontColor(Dempet);
                    }
                    else
                    {
                        bildeKol.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                for (var i = 0; i < 4; i++)
                                {
                                    c.RelativeColumn();
                                }
                            });
                            foreach (var b in bilder)
                            {
                                table.Cell().Padding(3).Column(c =>
                                {
                                    c.Item().Height(80).Border(0.75f).BorderColor(Linje).CornerRadius(6).Background(Annenhver).Padding(2)
                                        .AlignCenter().AlignMiddle().Image(b.Data).FitArea();
                                    c.Item().PaddingTop(2).Text(b.Plassering.Visningsnavn()).FontSize(7).FontColor(Dempet);
                                });
                            }
                        });
                    }
                    });
                });
                Bunn(page, ce, dornummer, org);
            });

            // ---------- Side 3: EU-samsvarserklæringen ----------
            doc.Page(page =>
            {
                Oppsett(page);
                SideHode(page, org, "EU-samsvarserklæring", $"{dornummer} · {ce.Sertifiseringsnummer}");
                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);
                    var produsent = string.Join(", ", new[]
                    {
                        ce.Produsent, ce.ProdusentAdresse,
                        string.Join(" ", new[] { ce.ProdusentPostnr, ce.ProdusentSted }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        ce.ProdusentLand
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    col.Item().Element(e => Boks(e, "1 · Produkt", c => Liste(c,
                    [
                        ("Produsent", string.IsNullOrWhiteSpace(ce.ProdusentOrgnr) ? produsent : $"{produsent} · Org.nr {ce.ProdusentOrgnr}"),
                        ("Type / modell", ce.ItemNavn),
                        ("Serienummer", ce.Serienummer),
                        ("Produksjonsår", ce.ProduksjonsAar?.ToString()),
                        ("Montert", $"Dør {dornummer}" + (string.IsNullOrWhiteSpace(ce.Adresse) ? "" : $" · {ce.ProsjektNavn}, {ce.Adresse}"))
                    ], bred: true)));

                    col.Item().Element(e => Boks(e, "2 · Installatør", c => Liste(c,
                    [
                        ("Foretak", org.OrgNr is null ? org.Navn : $"{org.Navn} · Org.nr {org.OrgNr}"),
                        ("Adresse", org.Adresse),
                        ("Kontakt", string.Join(" · ", new[] { org.Telefon, org.Epost }.Where(s => !string.IsNullOrWhiteSpace(s))))
                    ], bred: true)));

                    col.Item().Element(e => Boks(e, "3 · Erklæring", c =>
                    {
                        c.Item().Text("Vi erklærer under eget ansvar at dørautomatikken beskrevet over, slik den er montert og kontrollert, er i samsvar med:").LineHeight(1.35f);
                        c.Item().PaddingTop(6).Element(t => ToKolonner(t,
                        [
                            ("Maskindirektivet 2006/42/EF", "eller maskinforordningen (EU) 2023/1230 der den gjelder"),
                            ("Lavspenningsdirektivet 2014/35/EU", "der det er aktuelt"),
                            ("EMC-direktivet 2014/30/EU", "")
                        ]));
                    }));

                    col.Item().Element(e => Boks(e, "4 · Standarder", c => ToKolonner(c.Item(),
                    [
                        ("NS-EN 16005:2023 + A1:2024", "Motoriserte dørautomatikkanlegg for fotgjengere – sikkerhet ved bruk"),
                        ("NS-EN ISO 12100", "Risikovurdering og risikoreduksjon"),
                        ("NS-EN ISO 13849-1", "Sikkerhetsrelaterte deler av styresystemer, der det er aktuelt"),
                        ("NS-EN 60335-1 / NS-EN 61000", "Elsikkerhet og EMC, der det er aktuelt")
                    ])));

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        var sted = ce.Dor?.Prosjekt?.Sted;
                        row.RelativeItem().Element(e => Signatur(e, ce.UtfortAvSignatur, ce.UtfortAvNavn,
                            string.Join(", ", new[] { "Montør", sted, ce.UtfortAvDato?.ToString("dd.MM.yyyy") }.Where(s => !string.IsNullOrWhiteSpace(s)))));
                        row.ConstantItem(24);
                        row.RelativeItem().Element(e => Signatur(e, ce.VerifisertAvSignatur, ce.VerifisertAvNavn,
                            string.Join(", ", new[] { "Ansvarlig", ce.VerifisertAvDato?.ToString("dd.MM.yyyy") }.Where(s => !string.IsNullOrWhiteSpace(s)))));
                    });
                });
                Bunn(page, ce, dornummer, org);
            });
        });

        return document.GeneratePdf();
    }

    // ---------- Kravene ----------

    private static string Tall(double? v) => v?.ToString("0.##", Kultur) ?? "";

    private static Krav JaNei(string tekst, bool? verdi, bool? feilVed)
    {
        var resultat = verdi switch { true => "Ja", false => "Nei", null => null };
        var status = verdi is null ? KravStatus.IkkeSvart
            : feilVed is null ? KravStatus.Registrert
            : verdi == feilVed ? KravStatus.IkkeOppfylt
            : KravStatus.Oppfylt;
        return new Krav(tekst, resultat, null, status);
    }

    private static Krav Maling(string tekst, double? verdi, string enhet, double? maks, bool unntatt = false)
    {
        if (unntatt)
        {
            return new Krav(tekst, "Unntatt", null, KravStatus.Unntatt);
        }

        var grense = maks is null ? null : $"maks {Tall(maks)} {enhet}";
        var status = verdi is null ? KravStatus.IkkeSvart
            : maks is null ? KravStatus.Registrert
            : verdi <= maks ? KravStatus.Oppfylt
            : KravStatus.IkkeOppfylt;
        return new Krav(tekst, verdi is null ? null : $"{Tall(verdi)} {enhet}", grense, status);
    }

    private static List<Krav> KravDoren(CeGodkjenning ce)
    {
        var liste = new List<Krav>
        {
            JaNei("Fri passasjebredde minst 0,86 m", ce.FriBredde086, feilVed: false),
            JaNei("Terskelhøyde under 25 mm", ce.TerskelUnder25mm, feilVed: false),
            JaNei("Fare for kuttskade i dørmiljøet", ce.KuttskadeRisiko, feilVed: true),
            new("Glass i dør", ce.GlassIDor, null, string.IsNullOrWhiteSpace(ce.GlassIDor) ? KravStatus.IkkeSvart : KravStatus.Registrert)
        };
        if (ce.GlassIDor == "Med glass")
        {
            liste.Add(new Krav("Glasstype", ce.TypeAvGlass, null, string.IsNullOrWhiteSpace(ce.TypeAvGlass) ? KravStatus.IkkeSvart : KravStatus.Registrert));
            liste.Add(JaNei("Glasset har synlighetstiltak", ce.GlassSynligTiltak, feilVed: false));
            liste.Add(JaNei("Fare for kutt og skade fra glasset", ce.GlassFareKuttSkade, feilVed: true));
        }

        return liste;
    }

    private static List<Krav> KravMal(CeGodkjenning ce, CeMaleGrenseverdier g) =>
    [
        Maling("Åpningstid 0–80°", ce.ApningstidSek, "s", g.MaksApningstidSek, ce.ApningstidUnntatt),
        Maling("Lukketid 90–10°", ce.LukketidHoySek, "s", g.MaksLukketidHoySek, ce.LukketidHoyUnntatt),
        Maling("Lukketid 10–0°", ce.LukketidLavSek, "s", g.MaksLukketidLavSek, ce.LukketidLavUnntatt),
        Maling("Åpningskraft", ce.ApningskraftN, "N", g.MaksApningskraftN, ce.ApningskraftUnntatt),
        Maling("Åpningsvinkel hoveddørblad", ce.Apningsvinkel, "°", null),
        Maling("Avstand til trapp, hengselside", ce.AvstandTrappCm, "cm", null, ce.AvstandTrappUnntatt),
        Maling("Avstand til vegg, hengselside", ce.AvstandVeggCm, "cm", null, ce.AvstandVeggUnntatt),
        JaNei("Dødgang etter stopp under 50 cm", ce.DodlasEtterStopp, feilVed: false),
        JaNei("Forsinkelse før lukking minst 3 s", ce.ForsinkelseForLukking, feilVed: false),
        JaNei("Åpnes mot område med gjennomgangstrafikk", ce.ApnesMotGjennomgangstrafikk, feilVed: null)
    ];

    private static List<Krav> KravSikkerhet(CeGodkjenning ce)
    {
        var liste = new List<Krav>
        {
            new("Dekningsområde hoveddørblad", ce.DekningsomradeHovedDorblad, null,
                string.IsNullOrWhiteSpace(ce.DekningsomradeHovedDorblad) ? KravStatus.IkkeSvart : KravStatus.Registrert)
        };
        if (ce.DekningsomradeHovedDorblad == "I henhold til vedlegg G")
        {
            liste.Add(Maling("Beskyttet bredde hoveddørblad", ce.BeskyttetBreddeHovedDorbladMm, "mm", null));
        }

        liste.AddRange(
        [
            JaNei("Sensorplassering korrekt iht. instruksjoner", ce.SensorplasseringKorrekt, feilVed: false),
            JaNei("Reaksjonstid tilfredsstillende", ce.ReaksjonstidOk, feilVed: false),
            JaNei("Har impulssensor IR eller laser", ce.HarImpulssensorIrLaser, feilVed: null),
            JaNei("Sikkerhetssensor kobles ut ved brannalarm", ce.SikkerhetssensorUtkoblingBrannalarm, feilVed: false),
            JaNei("Nødåpning ved brannalarm testet", ce.NodapningTestet, feilVed: null),
            JaNei("Impulsgivere i riktig høyde og tilgjengelige", ce.ImpulsbryterKorrektHoyde, feilVed: false),
            JaNei("Betjeningsbrytere med fri sideplass", ce.AktiveringsbryterFriPlass, feilVed: false),
            JaNei("Klar merking for alle brukergrupper", ce.TydeligSkilting, feilVed: false),
            JaNei("Beskyttelse av hengselareal", ce.HengselsideBeskyttet, feilVed: false),
            JaNei("Elektronisk låskobling testet", ce.ElektroniskLasKoblingTestet, feilVed: false)
        ]);
        return liste;
    }

    // ---------- Byggeklosser ----------

    private static void Oppsett(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(36);
        page.MarginTop(30);
        page.MarginBottom(24);
        page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Ink));
    }

    private void SideHode(PageDescriptor page, RapportProfil org, string tittel, string under)
    {
        page.Header().BorderBottom(0.75f).BorderColor(Linje).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Height(26).AlignMiddle().Element(e => org.TegnLogo(e, pdfLogo, 22));
            row.RelativeItem().AlignRight().AlignMiddle().Column(c =>
            {
                c.Item().AlignRight().Text(tittel).FontSize(9.5f).Bold();
                c.Item().AlignRight().Text(under).FontSize(7.5f).FontColor(Dempet);
            });
        });
    }

    private static void Bunn(PageDescriptor page, CeGodkjenning ce, string dornummer, RapportProfil org)
    {
        page.Footer().BorderTop(0.75f).BorderColor(Linje).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text($"{ce.Sertifiseringsnummer} · Dør {dornummer}").FontSize(7).FontColor(Dempet);
            row.RelativeItem().AlignCenter().Text($"{org.Navn} · Laget i Full Kontroll {DateTime.Now:dd.MM.yyyy}").FontSize(7).FontColor(Dempet);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Dempet));
                t.Span("Side ");
                t.CurrentPageNumber();
                t.Span(" av ");
                t.TotalPages();
            });
        });
    }

    private static void Fakta(RowDescriptor row, string navn, string? verdi, bool forste = false)
    {
        var celle = row.RelativeItem();
        if (!forste)
        {
            celle = celle.BorderLeft(0.75f).BorderColor(Linje);
        }

        celle.PaddingVertical(8).PaddingHorizontal(10).Column(c =>
        {
            c.Item().Text(navn.ToUpperInvariant()).FontSize(6.5f).Bold().LetterSpacing(0.07f).FontColor(Dempet);
            c.Item().PaddingTop(1).Text(string.IsNullOrWhiteSpace(verdi) ? "-" : verdi).FontSize(9.5f).Bold();
        });
    }

    private static void Boks(IContainer container, string tittel, Action<ColumnDescriptor> innhold)
    {
        container.Border(0.75f).BorderColor(Linje).CornerRadius(8).Column(c =>
        {
            c.Item().Background(Hode).BorderBottom(0.75f).BorderColor(Linje).PaddingVertical(6).PaddingHorizontal(10)
                .Text(tittel.ToUpperInvariant()).FontSize(7.5f).Bold().LetterSpacing(0.07f);
            c.Item().Padding(10).Column(innhold);
        });
    }

    private static void Liste(ColumnDescriptor col, (string Navn, string? Verdi)[] rader, bool bred = false)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(bred ? 90 : 72);
                c.RelativeColumn();
            });
            foreach (var (navn, verdi) in rader)
            {
                table.Cell().PaddingVertical(1.5f).Text(navn).FontColor(Dempet);
                table.Cell().PaddingVertical(1.5f).Text(string.IsNullOrWhiteSpace(verdi) ? "-" : verdi).SemiBold();
            }
        });
    }

    private static void ToKolonner(IContainer container, (string Fet, string Tekst)[] rader)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(3);
            });
            var i = 0;
            foreach (var (fet, tekst) in rader)
            {
                var bakgrunn = i++ % 2 == 1 ? Annenhver : Colors.White;
                table.Cell().Background(bakgrunn).BorderBottom(0.5f).BorderColor(LinjeLys).Padding(5).Text(fet).SemiBold();
                table.Cell().Background(bakgrunn).BorderBottom(0.5f).BorderColor(LinjeLys).Padding(5).Text(tekst).FontColor(Dempet);
            }
        });
    }

    private static void KravTabell(IContainer container, (string Gruppe, List<Krav> Krav)[] grupper, bool medGruppe)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4.2f);
                c.RelativeColumn(1.6f);
                c.RelativeColumn(1.6f);
                c.ConstantColumn(78);
            });
            table.Header(h =>
            {
                IContainer Celle(IContainer c) => c.Background(Hode).BorderBottom(0.75f).BorderColor(Color.FromHex("#D9DEE6")).PaddingVertical(5).PaddingHorizontal(7);
                h.Cell().Element(Celle).Text("KRAV").FontSize(6.5f).Bold().LetterSpacing(0.06f).FontColor(Dempet);
                h.Cell().Element(Celle).Text("RESULTAT").FontSize(6.5f).Bold().LetterSpacing(0.06f).FontColor(Dempet);
                h.Cell().Element(Celle).Text("GRENSE").FontSize(6.5f).Bold().LetterSpacing(0.06f).FontColor(Dempet);
                h.Cell().Element(Celle).AlignRight().Text("STATUS").FontSize(6.5f).Bold().LetterSpacing(0.06f).FontColor(Dempet);
            });

            foreach (var (gruppe, krav) in grupper)
            {
                if (medGruppe)
                {
                    table.Cell().ColumnSpan(4).BorderBottom(0.75f).BorderColor(Color.FromHex("#D9DEE6")).PaddingTop(9).PaddingBottom(4).PaddingHorizontal(7)
                        .Text(gruppe.ToUpperInvariant()).FontSize(7).Bold().LetterSpacing(0.06f);
                }

                var i = 0;
                foreach (var k in krav)
                {
                    var bakgrunn = i++ % 2 == 1 ? Annenhver : Colors.White;
                    IContainer Celle(IContainer c) => c.Background(bakgrunn).BorderBottom(0.5f).BorderColor(LinjeLys).PaddingVertical(3.6f).PaddingHorizontal(7);
                    table.Cell().Element(Celle).Text(k.Tekst);
                    table.Cell().Element(Celle).Text(k.Resultat ?? "-").SemiBold();
                    table.Cell().Element(Celle).Text(k.Grense ?? "").FontColor(Dempet);
                    table.Cell().Element(Celle).AlignRight().Element(e => Status(e, k.Status));
                }
            }
        });
    }

    private static void Status(IContainer container, KravStatus status)
    {
        var (tekst, farge, bakgrunn) = status switch
        {
            KravStatus.Oppfylt => ("Oppfylt", Gronn, GronnLys),
            KravStatus.IkkeOppfylt => ("Ikke oppfylt", Rod, RodLys),
            KravStatus.IkkeSvart => ("Ikke svart", Gul, GulLys),
            KravStatus.Unntatt => ("Unntatt", Gra, GraLys),
            _ => ("Registrert", Gra, GraLys)
        };
        container.Background(bakgrunn).CornerRadius(8).PaddingVertical(1.5f).PaddingHorizontal(7)
            .Text(tekst).FontSize(7).Bold().FontColor(farge);
    }

    private static void TilbehorTabell(IContainer container, List<DorKomponent> rader)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3.4f);
                c.RelativeColumn(1.6f);
                c.RelativeColumn(1);
                c.RelativeColumn(0.9f);
                c.ConstantColumn(92);
            });
            table.Header(h =>
            {
                IContainer Celle(IContainer c) => c.Background(Hode).BorderBottom(0.75f).BorderColor(Color.FromHex("#D9DEE6")).PaddingVertical(5).PaddingHorizontal(7);
                foreach (var t in new[] { "VARE", "PRODUKTNR.", "ANTALL", "PLASS.", "DOKUMENT" })
                {
                    h.Cell().Element(Celle).Text(t).FontSize(6.5f).Bold().LetterSpacing(0.06f).FontColor(Dempet);
                }
            });
            var i = 0;
            foreach (var k in rader)
            {
                var bakgrunn = i++ % 2 == 1 ? Annenhver : Colors.White;
                IContainer Celle(IContainer c) => c.Background(bakgrunn).BorderBottom(0.5f).BorderColor(LinjeLys).PaddingVertical(3.6f).PaddingHorizontal(7);
                table.Cell().Element(Celle).Column(c =>
                {
                    c.Item().Text(k.Component?.Navn ?? "-").SemiBold();
                    if (k.Component?.Type?.CeKategori is { } kat)
                    {
                        c.Item().Text(kat.Visningsnavn()).FontSize(7).FontColor(Dempet);
                    }
                });
                table.Cell().Element(Celle).Text(k.Component?.Produktkode ?? "-");
                table.Cell().Element(Celle).Text($"{k.Antall} {k.Enhet}".Trim());
                table.Cell().Element(Celle).Text(k.Plassering?.Kode() ?? "-");
                var (dok, ok) = k.Component?.FdvFilnavn is not null ? ("FDV", true)
                    : k.Component?.Type?.CeDokumentData is not null ? ("Standarddokument", true)
                    : ("Mangler", false);
                table.Cell().Element(Celle).AlignLeft().Element(e => e.Background(ok ? GronnLys : GulLys).CornerRadius(8).PaddingVertical(1.5f).PaddingHorizontal(7)
                    .Text(dok).FontSize(7).Bold().FontColor(ok ? Gronn : Gul));
            }
        });
    }

    private static void Signatur(IContainer container, byte[]? signatur, string? navn, string under)
    {
        container.Column(c =>
        {
            var flate = c.Item().Height(42).BorderBottom(1).BorderColor(Ink);
            if (signatur is { Length: > 0 })
            {
                flate.PaddingBottom(2).AlignLeft().AlignBottom().Image(signatur).FitHeight();
            }

            c.Item().PaddingTop(4).Text(t =>
            {
                t.Span(string.IsNullOrWhiteSpace(navn) ? "-" : navn).Bold().FontSize(7.5f);
                if (!string.IsNullOrWhiteSpace(under))
                {
                    t.Span(" · " + under).FontSize(7.5f).FontColor(Dempet);
                }
            });
        });
    }

    private static string SvgHake(string farge) =>
        $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M4 12.5 9.5 18 20 6.5' fill='none' stroke='{farge}' stroke-width='3' stroke-linecap='round' stroke-linejoin='round'/></svg>";

    private static string SvgKryss(string farge) =>
        $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M6 6l12 12M18 6 6 18' fill='none' stroke='{farge}' stroke-width='3' stroke-linecap='round'/></svg>";
}
