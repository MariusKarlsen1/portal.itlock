using System.ComponentModel.DataAnnotations.Schema;

namespace PortalItlock.Web.Models;

public class Bruker
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public required string Epost { get; set; }
    public string? PasswordHash { get; set; }
    public string? Stilling { get; set; }
    public BrukerRolle Rolle { get; set; } = BrukerRolle.Montor;
    public bool Aktiv { get; set; } = true;

    public int FerieKvote { get; set; } = 25;
    public int? SisteNyhetSettId { get; set; }
    public int? SisteKunngjoringSettId { get; set; }
    public string? SkjulteNavLenker { get; set; }
    public string? TopplinjeSnarveier { get; set; }
    public string? EkstraNavLenker { get; set; }

    // Husker sist valgte visning (f.eks. "kanban"/"liste"/"kart") pr. modul,
    // lagret som ett JSON-objekt {"prosjekter":"kart","befaring":"rutenett",...}
    // - se VisningPreferanseService. På eksplisitt ønske 2026-10-09: valgt
    // visning skal være standard igjen neste gang man åpner modulen, til man
    // bytter tilbake.
    public string? VisningsPreferanser { get; set; }

    // Styrer hvilken av de tre Hjem-layoutene brukeren ser - velges via
    // "Layout N"-knappen i Tilpass meny (se TopBar.razor). Bilde er standard
    // (matcher tidligere HjemLayoutKlassisk=false), som bevart av migrasjonen
    // som la til dette feltet.
    public HjemLayout HjemLayoutValg { get; set; } = HjemLayout.Bilde;

    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public List<Arbeidsordre> Arbeidsordre { get; set; } = [];
    public List<Timeregistrering> Timeregistreringer { get; set; } = [];
    public List<DorKomponent> MonterteKomponenter { get; set; } = [];
    public List<Prosjekt> Prosjekter { get; set; } = [];

    [NotMapped]
    public string Initialer => string.Concat(
        Navn.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(del => char.ToUpperInvariant(del[0])));
}
