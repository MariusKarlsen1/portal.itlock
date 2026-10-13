using System.Security.Claims;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Hjelper for "Vis som Kunde / Driftslisens" i toppfeltet. En admin har ingen
// kundekobling og ingen prosjekter som vaktmester, så uten dette ville de to
// portalene bare vist "ikke koblet til en kunde" - man ser rammeverket, men
// ikke hva kunden faktisk ser.
//
// Løsningen er en ForhandsvisBrukerId-claim satt av /account/vis-som: den peker
// på en ekte kunde- eller driftslisensbruker som portalen leser innholdet til.
//
// Viktig: BrukerId-claimen forblir administratorens egen. Vi later ikke som vi
// ER den brukeren - vi leser bare det de ser. Alt som SKRIVER i de to
// portalene sjekker ErForhandsvisning og avvises, slik at en admin som klikker
// seg rundt ikke kan opprette en henvendelse eller godkjenne et tilvalg i
// kundens navn.
public static class ForhandsvisningHelper
{
    public const string ClaimNavn = "ForhandsvisBrukerId";

    // Brukeren portalen skal vise innholdet til, hvis vi er i forhåndsvisning.
    // Krever at den EKTE rollen er Admin - claimen alene er ikke nok.
    public static int? ForhandsvisBrukerId(this ClaimsPrincipal bruker)
    {
        if (bruker.FindFirst("EkteRolle")?.Value != nameof(BrukerRolle.Admin))
        {
            return null;
        }

        var verdi = bruker.FindFirst(ClaimNavn)?.Value;
        return int.TryParse(verdi, out var id) ? id : null;
    }

    public static bool ErForhandsvisning(this ClaimsPrincipal bruker) =>
        bruker.ForhandsvisBrukerId() is not null;

    // Id-en portalsiden skal lese data for: forhåndsvisningsmålet når det
    // finnes, ellers brukerens egen.
    public static int? VisningsBrukerId(this ClaimsPrincipal bruker)
    {
        if (bruker.ForhandsvisBrukerId() is { } forhandsvist)
        {
            return forhandsvist;
        }

        var egen = bruker.FindFirst("BrukerId")?.Value;
        return int.TryParse(egen, out var id) ? id : null;
    }
}
