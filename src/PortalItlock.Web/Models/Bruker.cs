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
    // "Layout N"-knappen i Tilpass meny (se TopBar.razor). Layout 3 (Fjord)
    // er standard: det er forsiden nye organisasjoner og nye brukere møter ved
    // første innlogging. Eksisterende brukere ble satt til den samme én gang
    // (migreringen SettFjordSomHjemLayoutForAlle); etterpå beholder alle sitt
    // eget valg. Layout 1 og 2 kan velges i Tilpass meny for den som vil.
    // Bilde-layouten er ikke lenger valgbar i bryteren, men rendres fortsatt.
    public HjemLayout HjemLayoutValg { get; set; } = HjemLayout.Fjord;

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
