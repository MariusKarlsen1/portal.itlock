namespace PortalItlock.Web.Models;

public class Component
{
    public int Id { get; set; }
    public int? ComponentTypeId { get; set; }
    public ComponentType? Type { get; set; }

    public required string Navn { get; set; }
    public string? Produsent { get; set; }
    public string? ProdusentAdresse { get; set; }
    public string? ProdusentPostnr { get; set; }
    public string? ProdusentSted { get; set; }
    public string? ProdusentLand { get; set; }
    public string? ProdusentOrgnr { get; set; }
    public string? Produktkode { get; set; }
    public string? Beskrivelse { get; set; }
    public string? Overflate { get; set; }
    public bool ErSylinder { get; set; }

    public string? Leverandor { get; set; }
    public string? Varegruppe { get; set; }
    public decimal? PrisNetto { get; set; }
    public decimal? PrisVeiledende { get; set; }

    public int? RabattgruppeId { get; set; }
    public Rabattgruppe? Rabattgruppe { get; set; }
    public int? MontasjeMinutterProsjekt { get; set; }
    public int? MontasjeMinutterArbeidsordre { get; set; }
    public int? MontasjeMinutterService { get; set; }
    public string? Enhet { get; set; }
    public bool Aktiv { get; set; } = true;

    public bool ILagerstyring { get; set; }
    public int Lagerantall { get; set; }
    public int? Minimumsbeholdning { get; set; }

    public byte[]? FdvData { get; set; }
    public string? FdvFilnavn { get; set; }
    public string? FdvContentType { get; set; }

    public List<PackageComponent> Pakker { get; set; } = [];
}
