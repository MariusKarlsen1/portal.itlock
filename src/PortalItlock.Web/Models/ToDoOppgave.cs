namespace PortalItlock.Web.Models;

public class ToDoOppgave
{
    public int Id { get; set; }

    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }

    public required string Tittel { get; set; }
    public string? Gruppe { get; set; }
    public bool Fullfort { get; set; }

    // Når oppgaven ble huket av - brukes til Utført-arkivet (år, måned, uke).
    // Er null for oppgaver som ble fullført før feltet fantes.
    public DateTime? FullfortDato { get; set; }
    public ToDoPrioritet Prioritet { get; set; } = ToDoPrioritet.Normal;
    public DateTime? Frist { get; set; }
    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}
