using System.ComponentModel.DataAnnotations.Schema;

namespace PortalItlock.Web.Models;

public class Avvik
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    // Løpenummer i organisasjonen (AVV-0005). Settes ved lagring, se
    // ApplicationDbContext.NummererAvvik.
    public int Nummer { get; set; }

    // Kort tittel som vises i lister og e-post. Eldre avvik har bare beskrivelse.
    public string? Tittel { get; set; }
    public required string Beskrivelse { get; set; }
    public string? UtbedringBeskrivelse { get; set; }
    public decimal? Pris { get; set; }
    public AvvikStatus Status { get; set; } = AvvikStatus.Apent;
    public AvvikPrioritet Prioritet { get; set; } = AvvikPrioritet.Normal;
    public bool SynligForKunde { get; set; }

    // Den som skal følge opp avviket og får e-posten. Tom = prosjektlederen.
    public int? AnsvarligBrukerId { get; set; }
    public Bruker? AnsvarligBruker { get; set; }

    public int? ArbeidsordreId { get; set; }
    public Arbeidsordre? Arbeidsordre { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }

    public DateTime? SendtTilKundeDato { get; set; }
    public DateTime? LukketDato { get; set; }

    public byte[]? Signatur { get; set; }
    public string? SignertAvNavn { get; set; }
    public DateTime? SignertDato { get; set; }

    public List<AvvikMedia> Media { get; set; } = [];
    public List<AvvikKommentar> Kommentarer { get; set; } = [];

    [NotMapped]
    public string NummerTekst => Nummer > 0 ? $"AVV-{Nummer:0000}" : "Avvik";

    [NotMapped]
    public string Overskrift => !string.IsNullOrWhiteSpace(Tittel)
        ? Tittel
        : Beskrivelse.Length > 70 ? Beskrivelse[..70].TrimEnd() + "…" : Beskrivelse;
}

public enum AvvikPrioritet
{
    Lav,
    Normal,
    Hoy
}

public static class AvvikPrioritetExtensions
{
    public static string Visningsnavn(this AvvikPrioritet p) => p switch
    {
        AvvikPrioritet.Lav => "Lav",
        AvvikPrioritet.Hoy => "Høy",
        _ => "Normal"
    };
}

// Bilder og filer på et avvik. Rotasjon (0/90/180/270) lagres og brukes både
// i portalen og i PDF-en.
public class AvvikMedia
{
    public int Id { get; set; }
    public int AvvikId { get; set; }
    public Avvik? Avvik { get; set; }
    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public required string Filnavn { get; set; }
    public int Rotasjon { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}

// Loggen på avviket: kommentarer og hendelser (meldt, lukket, sendt til kunde …).
public class AvvikKommentar
{
    public int Id { get; set; }
    public int AvvikId { get; set; }
    public Avvik? Avvik { get; set; }
    public required string Tekst { get; set; }
    public bool ErHendelse { get; set; }
    public int? BrukerId { get; set; }
    public Bruker? Bruker { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;
}
