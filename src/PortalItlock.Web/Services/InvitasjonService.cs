using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Sender invitasjon til en bruker i en organisasjon: en e-post med lenke for
// å sette passord. Lenken varer i 7 dager, og bruker samme sikre flyt som
// «Glemt passord» (/tilbakestill-passord).
public class InvitasjonService(EmailService epost, PlattformOppsettService oppsett)
{
    public async Task<string?> SendAsync(Tenant tenant, int brukerId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(tenant.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        var bruker = await db.Brukere.FirstOrDefaultAsync(b => b.Id == brukerId && b.Aktiv && b.SlettetDato == null);
        if (bruker is null)
        {
            return "Fant ikke brukeren.";
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.BrukerPasswordResetTokener.Add(new BrukerPasswordResetToken
        {
            BrukerId = bruker.Id,
            Token = token,
            UtlopsDato = DateTime.Now.AddDays(7)
        });
        await db.SaveChangesAsync();

        var inn = await oppsett.HentAsync();
        var lenke = $"{inn.PortalAdresse.TrimEnd('/')}/tilbakestill-passord?token={token}";
        var firma = WebUtility.HtmlEncode(tenant.PortalVisningsnavn ?? tenant.Navn);
        var html =
            $"<p>Hei {WebUtility.HtmlEncode(bruker.Navn.Split(' ')[0])},</p>" +
            $"<p>Du er invitert til Full Kontroll hos {firma}. Full Kontroll samler prosjekter, tilbud, arbeidsordre, timer og service på ett sted.</p>" +
            $"<p><a href=\"{lenke}\" style=\"display:inline-block;padding:10px 18px;background:#8A5A34;color:#ffffff;border-radius:8px;text-decoration:none;font-weight:bold\">Sett passord og logg inn</a></p>" +
            $"<p>Brukernavnet ditt er {WebUtility.HtmlEncode(bruker.Epost)}. Lenken virker i 7 dager. Etterpå logger du inn på {WebUtility.HtmlEncode(inn.PortalAdresse)}.</p>" +
            "<p>Hilsen Full Kontroll</p>";

        var sendt = await epost.SendAsync(bruker.Epost, $"Velkommen til Full Kontroll – {tenant.PortalVisningsnavn ?? tenant.Navn}", html, fraOrganisasjonId: tenant.Id);
        return sendt ? null : "E-posten ble ikke sendt. Sjekk Resend-nøkkelen under Innstillinger.";
    }
}
