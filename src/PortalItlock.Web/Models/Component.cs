namespace PortalItlock.Web.Models;

public class Component
{
    public int Id { get; set; }
    public int? ComponentTypeId { get; set; }
    public ComponentType? Type { get; set; }

    public required string Navn { get; set; }
    public string? Navn2 { get; set; }
    public string? Produsent { get; set; }
    public string? Konsept { get; set; }
    public string? ProdusentAdresse { get; set; }
    public string? ProdusentPostnr { get; set; }
    public string? ProdusentSted { get; set; }
    public string? ProdusentLand { get; set; }
    public string? ProdusentOrgnr { get; set; }
    public string? Produktkode { get; set; }
    public string? Gtin { get; set; }
    public string? Beskrivelse { get; set; }
    public string? Overflate { get; set; }
    public bool ErSylinder { get; set; }

    public string? Leverandor { get; set; }
    public string? Varegruppe { get; set; }
    public decimal? PrisNetto { get; set; }
    public decimal? PrisVeiledende { get; set; }
    public int? GarantitidManeder { get; set; }

    public int? RabattgruppeId { get; set; }
    public Rabattgruppe? Rabattgruppe { get; set; }

    // Sperrer kunderabatt (KundeRabatt) på denne varen i tilbud - både
    // auto-utfylling og manuell inntasting av TilbudLinje.RabattProsent.
    // Se TilbudSkjema.razor: HentKundeRabattProsent/OppdaterLinjePris.
    public bool IngenRabattTilgjengelig { get; set; }
    public List<Produktgruppe> Produktgrupper { get; set; } = [];
    public int? MontasjeMinutterProsjekt { get; set; }
    public int? MontasjeMinutterArbeidsordre { get; set; }
    public int? MontasjeMinutterService { get; set; }

    // Kun relevant for varer med programmerbar elektronikk (låser,
    // adgangskontroll o.l.) - vises derfor skjult bak en "+"-knapp på
    // varekortet i stedet for å ligge synlig på alle varer, se
    // KomponentPanel.razor.
    public int? ProgrammeringstidMinutter { get; set; }
    public string? Enhet { get; set; }
    public bool Aktiv { get; set; } = true;

    public bool ILagerstyring { get; set; }
    public int Lagerantall { get; set; }
    public int? Minimumsbeholdning { get; set; }

    public byte[]? FdvData { get; set; }
    public string? FdvFilnavn { get; set; }
    public string? FdvContentType { get; set; }

    // Flere FDV-dokumenter utover hoveddokumentet over - se
    // Models/ComponentFdvDokument.cs.
    public List<ComponentFdvDokument> FdvDokumenter { get; set; } = [];

    public byte[]? MontasjebladData { get; set; }
    public string? MontasjebladFilnavn { get; set; }
    public string? MontasjebladContentType { get; set; }

    // Flere montasjeblad utover hoveddokumentet over - se
    // Models/ComponentMontasjebladDokument.cs.
    public List<ComponentMontasjebladDokument> MontasjebladDokumenter { get; set; } = [];

    public byte[]? DatabladData { get; set; }
    public string? DatabladFilnavn { get; set; }
    public string? DatabladContentType { get; set; }

    // Flere datablad utover hoveddokumentet over - se
    // Models/ComponentDatabladDokument.cs.
    public List<ComponentDatabladDokument> DatabladDokumenter { get; set; } = [];

    // Vist på vare-panelet og som hover-forhåndsvisning i tilbud, se
    // /komponent/{id}/bilde. BildeStorrelse er brukerens ønskede
    // visningsstørrelse i piksler (høyeste/breddeste kant) - null gir
    // standardstørrelsen definert i CSS.
    public byte[]? BildeData { get; set; }
    public string? BildeFilnavn { get; set; }
    public string? BildeContentType { get; set; }
    public int? BildeStorrelse { get; set; }

    // Inntektskontoen varen skal bokføres på ved salg - portalens eget
    // kontoregister (Models/Inntektskonto.cs), ikke hentet fra Tripletex.
    // Brukes av ArbeidsordreOkonomiBeregner til å gruppere Resultatrapporten.
    public int? InntektskontoId { get; set; }
    public Inntektskonto? Inntektskonto { get; set; }

    // Manuelt satt via "Komplett"/"Dobbelsjekk"-knappene på varekortet - gir
    // grønn/oransj sirkel foran Rediger-knappen på vareregisteret, se
    // Komponenter.razor. Rent visuelt hjelpemiddel, brukes ikke i logikk.
    public bool ErKomplett { get; set; }
    public bool TrengerDobbelsjekk { get; set; }

    public List<PackageComponent> Pakker { get; set; } = [];

    // Leverandørene som selger denne varen, hver med sitt eget varenummer og
    // pris - se Models/ComponentLeverandor.cs og Services/LeverandorSync.cs.
    // Leverandor/Produktkode/PrisNetto over speiler alltid den som har
    // ErStandard=true her.
    public List<ComponentLeverandor> Leverandorer { get; set; } = [];
}
