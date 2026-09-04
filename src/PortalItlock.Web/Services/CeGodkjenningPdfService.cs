using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class CeGodkjenningPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2";
    private const string FirmaPostnr = "4374";
    private const string FirmaSted = "Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    private static readonly Color Sand = Color.FromHex("#F2EBE1");
    private static readonly Color SandBorder = Color.FromHex("#E4D9C8");
    private static readonly Color Accent = Color.FromHex("#835E41");
    private static readonly Color Ink = Color.FromHex("#292927");

    public async Task<byte[]?> GenerateAsync(int ceGodkjenningId)
    {
        var ce = await db.CeGodkjenninger
            .Include(c => c.Dor)
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

        var sikkerhet = dorKomponenter.Where(k => k.Component?.Type?.CeKategori == CeTilbehorKategori.SikkerhetsTilbehor).ToList();
        var annet = dorKomponenter.Where(k => k.Component?.Type?.CeKategori == CeTilbehorKategori.AnnetTilbehor).ToList();
        var validering = CeValideringsService.Evaluer(ce, dorKomponenter);
        var godkjent = validering.All(v => v.Bestatt);

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.6f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(75).AlignMiddle().Text("CE").FontSize(32).Bold().FontColor(Colors.Grey.Lighten1).LetterSpacing(0.05f);
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("Sertifikat for CE-samsvar").FontSize(16).Bold();
                            c.Item().Text(ce.Sertifiseringsnummer).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span(ce.Status.Visningsnavn()).FontSize(8.5f).Bold().FontColor(Accent);
                            });
                        });
                        row.ConstantItem(150).AlignRight().Element(e => pdfLogo.Render(e, 32));
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(SandBorder);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(4);

                    Overskrift(col, "Kunde informasjon");
                    FeltGrid(col, 2,
                    [
                        ("Kunde", ce.KundeNavn), ("Etasje", ce.Etasje),
                        ("Kontaktperson", ce.Kontaktperson), ("Bygg", ce.Bygg),
                        ("Prosjekt", ce.ProsjektNavn), ("Bygningskategori", ce.Byggkategori),
                        ("Arbeidsordre", ce.Arbeidsordre), ("Bygnings risikoklasse", ce.Risikoklasse),
                        ("Serviceavtale", JaNei(ce.Serviceavtale)), ("Dør til", ce.DorTil),
                        ("Bygning adresse", ce.Adresse), ("Dørnr.", ce.Dornummer),
                        ("Serviceadr.", ce.Serviceadresse), ("Dør ID", ce.DorIdKode)
                    ]);

                    Overskrift(col, "Maskindetaljer");
                    col.Item().Text("Produktdetaljer").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                    FeltGrid(col, 2,
                    [
                        ("Produsent", ce.Produsent), ("Antall", ce.Antall?.ToString()),
                        ("Produktnavn", ce.ItemNavn), ("Produksjonsår", ce.ProduksjonsAar?.ToString())
                    ]);

                    TilbehorTabell(col, "Sikkerhetsutstyr", sikkerhet);
                    TilbehorTabell(col, "Annet utstyr", annet);

                    Overskrift(col, "Dørdesign");
                    FeltGrid(col, 3,
                    [
                        ("Hoved dør, mm", ce.BreddeMm?.ToString()), ("Dørkonstruksjon", ce.Dorkonstruksjon), ("Brannklassifisering", ce.Brannklasse),
                        ("Dørbladhøyde, mm", ce.HoydeMm?.ToString()), ("Karmkonstruksjon", ce.Karmkonstruksjon), ("Terskelhøyde <25mm", JaNei(ce.TerskelUnder25mm)),
                        ("Hoved dørvekt, kg", ce.VektKg?.ToString()), ("Glass i dør", JaNei(ce.GlassIDor)), ("Fri passasjebredde ≥0,86m", JaNei(ce.FriBredde086)),
                        ("Glass har synlig tiltak", JaNei(ce.GlassSynligTiltak)), ("Glass fare for kutt og skade", JaNei(ce.GlassFareKuttSkade)), ("Fare for kutt/skade i dørmiljø", JaNei(ce.KuttskadeRisiko)),
                        ("Farge karm", ce.FargeKarm), ("Farge dørblad", ce.FargeDorblad), ("Karmtype", ce.Karmtype),
                        ("Terskel", ce.Terskeltype), ("Sparkeplate", ce.Sparkeplate), ("A mål", ce.AMal?.ToString()),
                        ("B mål", ce.BMal?.ToString()), ("Dørblad", ce.Dorblad), ("Glasstykkelse eller dørtype", ce.Glasstykkelse)
                    ]);

                    Overskrift(col, "Målinger");
                    FeltGrid(col, 3,
                    [
                        ("Åpningskraft, N", MalVerdi(ce.ApningskraftN, ce.ApningskraftUnntatt)),
                        ("Åpnes mot gjennomgangstrafikk", JaNei(ce.ApnesMotGjennomgangstrafikk)),
                        ("Kommentarer", ce.MalKommentar)
                    ]);
                    col.Item().Text("Hoved dør").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                    FeltGrid(col, 3,
                    [
                        ("Åpningsvinkel, °", ce.Apningsvinkel?.ToString()), ("Åpningstid, sek", MalVerdi(ce.ApningstidSek, ce.ApningstidUnntatt)), ("Lukketid 90-10°, sek", MalVerdi(ce.LukketidHoySek, ce.LukketidHoyUnntatt)),
                        ("Lukketid 10-0°, sek", MalVerdi(ce.LukketidLavSek, ce.LukketidLavUnntatt)), ("Dødgang etter stopp <50cm", JaNei(ce.DodlasEtterStopp)), ("Forsinkelsestid før lukking ≥3s", JaNei(ce.ForsinkelseForLukking)),
                        ("Avstand trapp, cm", MalVerdi(ce.AvstandTrappCm, ce.AvstandTrappUnntatt)), ("Avstand vegg, cm", MalVerdi(ce.AvstandVeggCm, ce.AvstandVeggUnntatt))
                    ]);

                    Overskrift(col, "Kontroll");
                    FeltGrid(col, 2,
                    [
                        ("Sensorplassering korrekt iht. instruksjoner", JaNei(ce.SensorplasseringKorrekt)), ("Dekningsområde hoveddørblad", ce.DekningsomradeHovedDorblad),
                        ("Beskyttet bredde hoveddørblad, mm", ce.DekningsomradeHovedDorblad == "According to annex G" ? ce.BeskyttetBreddeHovedDorbladMm?.ToString() : null),
                        ("Reaksjonstid tilfredsstillende (ingen forsinkelse)", JaNei(ce.ReaksjonstidOk)),
                        ("Sikkerhetssensor frigjøres ved brannalarm", JaNei(ce.SikkerhetssensorUtkoblingBrannalarm)), ("Nødåpningsfunksjon testet", JaNei(ce.NodapningTestet)),
                        ("Har impulssensor IR eller laser", JaNei(ce.HarImpulssensorIrLaser)),
                        ("Impulsgivere plassert i riktig høyde og tilgjengelighet", JaNei(ce.ImpulsbryterKorrektHoyde)), ("Betjeningsbrytere med tilstrekkelig fri sideplass", JaNei(ce.AktiveringsbryterFriPlass)),
                        ("Klar merking for alle brukergrupper", JaNei(ce.TydeligSkilting)), ("Hengselarealets beskyttelse", JaNei(ce.HengselsideBeskyttet)),
                        ("Elektronisk låskobling testet", JaNei(ce.ElektroniskLasKoblingTestet)), ("Ekstra funksjoner testet og logget", ce.EkstraFunksjonerKommentar)
                    ]);

                    Overskrift(col, "Funksjonstest");
                    FeltGrid(col, 2,
                    [
                        ("Fotografering ikke tillatt", ce.FotograferingIkkeTillatt ? "Ja" : "Nei"),
                        ("Antall mediefiler", ce.Media.Count.ToString())
                    ]);

                    Overskrift(col, "Signering");
                    FeltGrid(col, 2,
                    [
                        ("Utført den", ce.UtfortAvDato?.ToString("dd.MM.yyyy HH:mm")), ("Utfører", ce.UtfortAvNavn),
                        ("Gjennomgått den", ce.VerifisertAvDato?.ToString("dd.MM.yyyy HH:mm")), ("Gjennomganger", ce.VerifisertAvNavn)
                    ]);

                    col.Item().PageBreak();

                    Overskrift(col, "Validering");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(1); });
                        foreach (var v in validering)
                        {
                            table.Cell().PaddingVertical(3).Text(v.Tekst);
                            table.Cell().PaddingVertical(2).AlignRight().Element(e => Pill(e, v.Bestatt ? "Godkjent" : "Ikke godkjent", v.Bestatt));
                        }
                    });

                    col.Item().PageBreak();

                    col.Item().Text("CE egenerklæringsskjema").FontSize(15).Bold();
                    col.Item().Text("I samsvar med gjeldende EU-lovgivning").FontSize(9).FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Produsent").FontSize(11).Bold();
                            FeltGrid(c, 1,
                            [
                                ("Firmanavn", ce.Produsent), ("Adresse", ce.ProdusentAdresse), ("Postnummer", ce.ProdusentPostnr),
                                ("Sted", ce.ProdusentSted), ("Land", ce.ProdusentLand), ("Org.nr.", ce.ProdusentOrgnr)
                            ]);
                        });
                        row.ConstantItem(16);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Montør").FontSize(11).Bold();
                            FeltGrid(c, 1,
                            [
                                ("Foretaksnavn", FirmaNavn), ("Adresse", FirmaAdresse), ("Postnummer", FirmaPostnr),
                                ("Sted", FirmaSted), ("Land", "Norge"), ("Telefon / e-post", $"{FirmaTelefon} / {FirmaEpost}")
                            ]);
                        });
                    });

                    Overskrift(col, "1. Produktbeskrivelse");
                    FeltGrid(col, 1,
                    [
                        ("Produsent", ce.Produsent), ("Type/Modell", ce.ItemNavn),
                        ("Serienummer", ce.Serienummer), ("Produksjonsår", ce.ProduksjonsAar?.ToString())
                    ]);

                    Overskrift(col, "2. Erklæring");
                    col.Item().Text("Vi erklærer herved, under vårt eneansvar, at produktet beskrevet ovenfor er i samsvar med kravene i følgende EU-direktiver / forordninger:");
                    Punktliste(col,
                    [
                        ("Maskindirektivet 2006/42/EF", "(eller Maskinforordning (EU) 2023/1230, der det er aktuelt)"),
                        ("Lavspenningsdirektivet 2014/35/EU", "(der det er aktuelt)"),
                        ("EMC-direktivet 2014/30/EU", null)
                    ]);

                    Overskrift(col, "3. Anvendte harmoniserte standarder");
                    col.Item().Text("Følgende harmoniserte standarder er anvendt, helt eller delvis, for å dokumentere samsvar:");
                    Punktliste(col,
                    [
                        ("EN 16005:2023 + A1:2024", "Motoriserte dørautomatikkanlegg for fotgjengere – Sikkerhet ved bruk – Krav og prøvingsmetoder")
                    ]);
                    col.Item().PaddingTop(4).Text("Der det er aktuelt:").Italic();
                    Punktliste(col,
                    [
                        ("EN ISO 12100", "Risikovurdering og risikoreduksjon"),
                        ("EN 60335-1 / EN 61000-serien", "(der det er aktuelt)"),
                        ("EN 13849-1", "Sikkerhetsrelaterte deler av styresystemer (der det er aktuelt)")
                    ]);

                    Overskrift(col, "Signatur");
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            FeltGrid(c, 1,
                            [
                                ("Sted", ce.Dor?.Prosjekt?.Sted ?? FirmaSted), ("Dato", ce.UtfortAvDato?.ToString("dd.MM.yyyy")),
                                ("Navn", ce.UtfortAvNavn), ("Stilling", "Montør")
                            ]);
                            SignaturBoks(c, "Signatur", ce.UtfortAvSignatur);
                        });
                        row.ConstantItem(16);
                        row.RelativeItem().Column(c =>
                        {
                            FeltGrid(c, 1,
                            [
                                ("Ansvarlig navn", ce.VerifisertAvNavn)
                            ]);
                            SignaturBoks(c, "Ansvarlig signatur", ce.VerifisertAvSignatur);
                        });
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text(ce.Sertifiseringsnummer).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignCenter().Text($"{FirmaNavn} - Generert av itlock Full Kontroll {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        t.Span(" / ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        t.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string? JaNei(bool? verdi) => verdi switch { true => "Ja", false => "Nei", null => null };

    private static string? MalVerdi(double? verdi, bool unntatt) =>
        unntatt ? "Unntatt CE-krav" : verdi?.ToString();

    private static void Overskrift(ColumnDescriptor col, string tekst) =>
        col.Item().PaddingTop(10).Text(tekst).FontSize(13).Bold();

    private static void FeltGrid(ColumnDescriptor col, int kolonner, (string Label, string? Verdi)[] felter)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                for (var i = 0; i < kolonner; i++)
                {
                    c.RelativeColumn();
                }
            });

            foreach (var (label, verdi) in felter)
            {
                table.Cell().Padding(3).Column(c =>
                {
                    c.Item().Text(label).FontSize(7.8f).SemiBold().FontColor(Accent);
                    c.Item().PaddingTop(1).Background(Sand).BorderBottom(1).BorderColor(SandBorder)
                        .Padding(4).Text(string.IsNullOrWhiteSpace(verdi) ? "-" : verdi).FontSize(9.3f);
                });
            }
        });
    }

    private static void Punktliste(ColumnDescriptor col, (string Fet, string? Detalj)[] punkter)
    {
        foreach (var (fet, detalj) in punkter)
        {
            col.Item().PaddingLeft(10).PaddingTop(2).Text(t =>
            {
                t.Span("•  ");
                t.Span(fet).Bold();
                if (!string.IsNullOrWhiteSpace(detalj))
                {
                    t.Span(" - " + detalj);
                }
            });
        }
    }

    private static void SignaturBoks(ColumnDescriptor col, string label, byte[]? signatur)
    {
        col.Item().PaddingTop(3).Text(label).FontSize(7.8f).SemiBold().FontColor(Accent);
        if (signatur is null)
        {
            col.Item().PaddingTop(1).Background(Sand).BorderBottom(1).BorderColor(SandBorder).Padding(4).Text("-").FontSize(9.3f);
        }
        else
        {
            col.Item().PaddingTop(1).Background(Sand).BorderBottom(1).BorderColor(SandBorder).Padding(4)
                .Height(60).Image(signatur).FitArea();
        }
    }

    private static void Pill(IContainer container, string tekst, bool positiv)
    {
        var bakgrunn = positiv ? Color.FromHex("#D3ECDB") : Color.FromHex("#F4D9D0");
        var farge = positiv ? Color.FromHex("#2B7A4B") : Color.FromHex("#B5502D");
        container.Background(bakgrunn).PaddingVertical(2).PaddingHorizontal(8).Text(tekst).FontSize(8).Bold().FontColor(farge);
    }

    private static void TilbehorTabell(ColumnDescriptor col, string tittel, List<DorKomponent> rader)
    {
        if (rader.Count == 0)
        {
            return;
        }

        col.Item().PaddingTop(6).Text(tittel).FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(3);
                c.RelativeColumn(1);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(2.5f);
            });
            table.Header(header =>
            {
                IContainer Hode() => header.Cell().BorderBottom(1).BorderColor(Accent).PaddingBottom(3).PaddingRight(4);
                Hode().Text("Produktnr.").FontSize(8).SemiBold();
                Hode().Text("Navn").FontSize(8).SemiBold();
                Hode().Text("Ant.").FontSize(8).SemiBold();
                Hode().Text("Plassering").FontSize(8).SemiBold();
                Hode().Text("Beslagstype").FontSize(8).SemiBold();
            });
            foreach (var k in rader)
            {
                IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(4);
                Rad().Text(k.Component?.Produktkode ?? "-");
                Rad().Text(k.Component?.Navn ?? "-");
                Rad().Text($"{k.Antall} {k.Enhet}");
                Rad().Text(k.Plassering?.Kode() ?? "-");
                Rad().Text(k.Component?.Type?.Navn ?? "-");
            }
        });
    }
}
