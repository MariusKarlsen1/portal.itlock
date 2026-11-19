using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Dupliserer et koblingsskjema med alt innhold (komponenter, streker, kabler,
// tekst og innstillinger), som utgangspunkt for et nytt anlegg som ligner.
// Kopien kobles ikke til prosjekt eller dør - det velges på nytt.
public static class KoblingsSkjemaKopi
{
    public static async Task<KoblingsSkjema?> KopierAsync(ApplicationDbContext db, int kildeId, string? navn = null)
    {
        var kilde = await db.KoblingsSkjemaer.AsNoTracking().FirstOrDefaultAsync(s => s.Id == kildeId);
        if (kilde is null)
        {
            return null;
        }
        var symboler = await db.KoblingsSymboler.AsNoTracking().Where(s => s.KoblingsSkjemaId == kildeId).ToListAsync();
        var streker = await db.KoblingsStreker.AsNoTracking().Where(s => s.KoblingsSkjemaId == kildeId).ToListAsync();
        var komponenter = await db.KoblingsSkjemaKomponenter.AsNoTracking().Where(k => k.KoblingsSkjemaId == kildeId).ToListAsync();

        var ny = new KoblingsSkjema
        {
            KategoriId = kilde.KategoriId,
            Navn = string.IsNullOrWhiteSpace(navn) ? kilde.Navn + " (kopi)" : navn.Trim(),
            Beskrivelse = kilde.Beskrivelse,
            Versjon = kilde.Versjon,
            Notater = kilde.Notater,
            Stil = kilde.Stil,
            VisRutenett = kilde.VisRutenett,
            SnapTilRutenett = kilde.SnapTilRutenett,
            VisKomponentnavn = kilde.VisKomponentnavn,
            OpprettetDato = DateTime.UtcNow
        };
        db.KoblingsSkjemaer.Add(ny);
        await db.SaveChangesAsync();

        // AsNoTracking-kopiene legges inn på nytt som nye rader (alle felt følger med).
        foreach (var s in symboler)
        {
            s.Id = 0;
            s.KoblingsSkjemaId = ny.Id;
            s.KoblingsSkjema = null;
            s.SymbolBibliotek = null;
        }
        foreach (var s in streker)
        {
            s.Id = 0;
            s.KoblingsSkjemaId = ny.Id;
            s.KoblingsSkjema = null;
        }
        foreach (var k in komponenter)
        {
            k.Id = 0;
            k.KoblingsSkjemaId = ny.Id;
            k.KoblingsSkjema = null;
        }
        db.KoblingsSkjemaKomponenter.AddRange(komponenter);
        db.KoblingsSymboler.AddRange(symboler);
        db.KoblingsStreker.AddRange(streker);
        await db.SaveChangesAsync();
        return ny;
    }
}
