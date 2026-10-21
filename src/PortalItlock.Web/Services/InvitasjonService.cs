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
        var firmanavn = tenant.PortalVisningsnavn ?? tenant.Navn;
        var fornavn = bruker.Navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? bruker.Navn;
        var utloper = DateTime.Now.AddDays(7);
        var html = EpostMal.Lag(tenant, new EpostMal.Innhold
        {
            Farge = EpostMal.Brun,
            Merke = "Velkommen",
            Tittel = $"Velkommen, {fornavn}",
            IntroHtml = $"Du har fått tilgang til {WebUtility.HtmlEncode(firmanavn)} i Full Kontroll. Her finner du ordre, timer, prosjekter og dokumentasjon på ett sted.",
            Fakta =
            [
                ("Brukernavn", WebUtility.HtmlEncode(bruker.Epost)),
                ("Rolle", WebUtility.HtmlEncode(bruker.Rolle.Visningsnavn())),
                ("Lenken gjelder", WebUtility.HtmlEncode("Til " + utloper.ToString("dddd d. MMMM", EpostMal.Nb))),
            ],
            KnappTekst = "Sett passord",
            KnappUrl = lenke,
            Tips = $"Etterpå logger du inn på {inn.PortalAdresse}. På mobilen kan du legge portalen på hjemskjermen, så fungerer den som en app.",
            Grunn = $"Du får denne e-posten fordi {firmanavn} har gitt deg tilgang. Har du ikke ventet den, kan du se bort fra den."
        });

        var sendt = await epost.SendAsync(bruker.Epost, $"Du er invitert til {firmanavn} i Full Kontroll", html, fraOrganisasjonId: tenant.Id);
        return sendt ? null : "E-posten ble ikke sendt. Sjekk Resend-nøkkelen under Innstillinger.";
    }
}
