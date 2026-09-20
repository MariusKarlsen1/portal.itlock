using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class LisensVarselService(ApplicationDbContext db, EmailService epost, ILogger<LisensVarselService> logger)
{
    private const string MottakerEpost = "marius@itlock.no";
    public static readonly TimeSpan VarselFrist = TimeSpan.FromDays(30);

    public async Task<List<KundeLisens>> FinnNaerUtlopAsync()
    {
        var terskel = DateTime.Today.Add(VarselFrist);
        return await db.KundeLisenser
            .Include(l => l.Kunde)
            .Where(l => l.UtlopsDato.Date <= terskel)
            .OrderBy(l => l.UtlopsDato)
            .ToListAsync();
    }

    public async Task<(bool Sendt, int Antall)> SjekkOgSendVarselAsync(bool tvingSending = false)
    {
        var logg = await db.LisensVarselSendt.FirstOrDefaultAsync();
        if (!tvingSending && logg is not null && logg.SistSendtDato.Date >= DateTime.Today)
        {
            return (false, 0);
        }

        var naerUtlop = await FinnNaerUtlopAsync();

        var sendt = false;
        if (naerUtlop.Count > 0)
        {
            var rader = string.Join("", naerUtlop
                .Select(l => $"<tr><td>{System.Net.WebUtility.HtmlEncode(l.Kunde?.Navn ?? "Ukjent kunde")}</td><td>{System.Net.WebUtility.HtmlEncode(l.Navn)}</td><td>{l.UtlopsDato:dd.MM.yyyy}</td></tr>"));

            var html = "<p>Følgende kundelisenser går ut innen 1 måned eller er allerede utløpt:</p>"
                + $"<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\"><tr><th>Kunde</th><th>Lisens</th><th>Utløpsdato</th></tr>{rader}</table>"
                + "<p>Se kundekortet i portalen for detaljer.</p>";

            sendt = await epost.SendAsync(MottakerEpost, $"Lisenser som går ut snart ({naerUtlop.Count})", html);
            if (!sendt)
            {
                logger.LogWarning("Klarte ikke å sende lisensvarsel til {Mottaker}.", MottakerEpost);
            }
        }

        if (sendt || naerUtlop.Count == 0)
        {
            if (logg is null)
            {
                logg = new LisensVarselSendt { SistSendtDato = DateTime.Today };
                db.LisensVarselSendt.Add(logg);
            }
            else
            {
                logg.SistSendtDato = DateTime.Today;
            }
            await db.SaveChangesAsync();
        }

        return (sendt, naerUtlop.Count);
    }
}
