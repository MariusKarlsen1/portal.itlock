namespace PortalItlock.Web.Models;

// Representerer en pågående tidsregistreringsøkt (mobil "Start"-knapp på
// Oppgaver) - finnes kun mens klokka går, og fjernes når økten fullføres
// (konvertert til en ekte Timeregistrering) eller avbrytes. Lagret i databasen
// (ikke bare et felt i komponenten) slik at økten overlever sidenavigasjon,
// refresh og bytte mellom enheter.
public class AktivTimeOkt
{
    public int Id { get; set; }

    public int BrukerId { get; set; }
    public Bruker? Bruker { get; set; }

    public DateTime StartTidspunkt { get; set; } = DateTime.Now;

    // Satt når økten akkurat nå er satt på pause - null når den går som normalt.
    public DateTime? PauseStartTidspunkt { get; set; }

    // Sum av alle FULLFØRTE pauser (ikke medregnet en eventuell pågående, se
    // PauseStartTidspunkt) i minutter.
    public int AkkumulertPauseMinutter { get; set; }
}
