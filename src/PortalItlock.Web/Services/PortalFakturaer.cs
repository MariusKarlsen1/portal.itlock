using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// Det som er fakturert, regnet fra portalen - ikke hentet fra Tripletex.
/// Tripletex brukes bare til selve faktureringen. Rapportene (dashbord,
/// fakturaoversikt, salg per kunde) bygger på dette:
///  - arbeidsordre markert «Fakturert» (beløpet som på Fakturerte ordrer),
///  - salg i kassa (kontantfaktura og kreditnota), eks. mva.
/// Samme form som Tripletex-fakturaene, så rapportene kan bruke dem likt.
/// OrdreNummer er «AO» for arbeidsordre og «Kassa» for kassasalg.
public static class PortalFakturaer
{
    public const string Arbeidsordre = "AO";
    public const string Kassa = "Kassa";

    public static async Task<(List<TripletexService.TripletexFaktura> Fakturaer, string? Feilmelding)> HentAsync(
        ApplicationDbContext db, DateTime fra, DateTime til)
    {
        var start = fra.Date;
        var slutt = til.Date.AddDays(1);

        var ordre = await db.Arbeidsordre.AsNoTracking()
            .Where(a => a.Status == ArbeidsordreStatus.Fakturert && a.FakturertDato != null
                && a.FakturertDato >= start && a.FakturertDato < slutt)
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.KodeLinjer)
            .Include(a => a.Tilbud!).ThenInclude(t => t.Linjer)
            .Include(a => a.Tilbud!).ThenInclude(t => t.KodeLinjer)
            .Include(a => a.EkstraTilbud).ThenInclude(x => x.Tilbud!).ThenInclude(t => t.Linjer)
            .Include(a => a.EkstraTilbud).ThenInclude(x => x.Tilbud!).ThenInclude(t => t.KodeLinjer)
            .Include(a => a.Varer)
            .AsSplitQuery()
            .ToListAsync();

        var liste = ordre.Select(a =>
        {
            var kunde = a.Kunde ?? a.Prosjekt?.Kunde;
            return new TripletexService.TripletexFaktura(a.Id, a.Id, a.FakturertDato, null, kunde?.Id, kunde?.Navn,
                ArbeidsordreBeregningHelper.BeregnVareSum(a), 0, false, Arbeidsordre);
        }).ToList();

        var salg = await db.KasseSalg.AsNoTracking()
            .Where(s => s.Tidspunkt >= start && s.Tidspunkt < slutt)
            .ToListAsync();
        liste.AddRange(salg.Select(s => new TripletexService.TripletexFaktura(-s.Id, s.Nummer, s.Tidspunkt, null, null,
            string.IsNullOrWhiteSpace(s.KundeNavn) ? "Kontantsalg" : s.KundeNavn,
            Math.Abs(s.Total - s.Mva), 0, s.Type == KasseSalgType.Kreditnota || s.Total < 0, Kassa)));

        return (liste.OrderByDescending(f => f.Dato).ToList(), null);
    }
}
