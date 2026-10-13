using System.Security.Claims;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Hjelpere for "Vis som" i toppfeltet (se /account/vis-som og TilbakeTilAdmin).
//
// "Vis som" er ekte impersonering: BrukerId-claimen er målbrukerens, så hele
// portalen oppfører seg som om DEN brukeren var innlogget - Min dag viser
// deres jobber, timene er deres timer, kundeportalen viser deres kunde. Det er
// hele poenget: man skal se nøyaktig det de ser når man utvikler og tester.
//
// Konsekvensen er at alt man gjør mens man ser portalen som en annen, lagres
// på den brukeren. Derfor står banneret fra TilbakeTilAdmin synlig i alle
// layoutene så lenge det varer.
public static class ForhandsvisningHelper
{
    // Administratorens egen id, tatt vare på da byttet skjedde. Null når man
    // er seg selv.
    public static int? AdminBrukerId(this ClaimsPrincipal bruker)
    {
        if (bruker.FindFirst("EkteRolle")?.Value != nameof(BrukerRolle.Admin))
        {
            return null;
        }

        var verdi = bruker.FindFirst("AdminBrukerId")?.Value;
        return int.TryParse(verdi, out var id) ? id : null;
    }

    // Ser man portalen som en annen bruker akkurat nå?
    public static bool SerSomAnnenBruker(this ClaimsPrincipal bruker) =>
        bruker.AdminBrukerId() is not null;

    // Brukeren portalen skal vise data for. Dette er alltid BrukerId-claimen -
    // ved impersonering ER den målbrukerens. Metoden finnes for at sidene skal
    // slippe å gjenta claim-oppslaget, og for å gjøre det tydelig i koden at
    // det er den VISTE brukeren det leses for.
    public static int? VisningsBrukerId(this ClaimsPrincipal bruker)
    {
        var verdi = bruker.FindFirst("BrukerId")?.Value;
        return int.TryParse(verdi, out var id) ? id : null;
    }
}
