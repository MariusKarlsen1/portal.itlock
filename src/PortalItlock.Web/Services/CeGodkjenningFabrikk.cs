using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Lager en ny CE-godkjenning for en dør, med data fra døra, prosjektet og
// dørautomatikken. Brukes fra dørsiden og dørredigeringen i portalen og fra
// montørappen, så alle tre fyller ut det samme.
public static class CeGodkjenningFabrikk
{
    // Løpenummer som CE-ITL-DOR-2610-0007.
    public static async Task<string> NesteNummerAsync(ApplicationDbContext db) =>
        $"CE-ITL-DOR-{DateTime.Now:yyMM}-{await db.CeGodkjenninger.CountAsync() + 1:0000}";

    // Døra må være lastet med Prosjekt (og Kunde), DorIdMal og Komponenter med Component og Type.
    public static CeGodkjenning ForDor(Dor dor, string nummer, int? brukerId)
    {
        var automatikk = dor.Komponenter.FirstOrDefault(k => k.Component?.Type?.CeKategori == CeTilbehorKategori.DorAutomatikk);
        var gyldighetMåneder = dor.Prosjekt?.CeGyldighetMåneder is > 0 ? dor.Prosjekt!.CeGyldighetMåneder : 12;
        var gyldigFra = DateTime.Now;

        return new CeGodkjenning
        {
            DorId = dor.Id,
            Sertifiseringsnummer = nummer,
            MerkeToken = NyttMerkeToken(),
            OpprettetAvBrukerId = brukerId,
            KundeNavn = dor.Prosjekt?.Kunde?.Navn,
            Kontaktperson = dor.Prosjekt?.Kontaktperson,
            ProsjektNavn = dor.Prosjekt?.Navn,
            Adresse = dor.Prosjekt?.Adresse,
            Etasje = dor.Etasje,
            DorTil = dor.DorTil,
            Dornummer = dor.Dornummer,
            DorIdKode = dor.DorIdMal?.Kode,
            Byggkategori = dor.Prosjekt?.Byggkategori,
            Risikoklasse = dor.Prosjekt?.Risikoklasse,
            GyldighetMåneder = gyldighetMåneder,
            GyldigFra = gyldigFra,
            GyldigTil = gyldigFra.AddMonths(gyldighetMåneder),
            Produsent = automatikk?.Component?.Produsent,
            ProdusentAdresse = automatikk?.Component?.ProdusentAdresse,
            ProdusentPostnr = automatikk?.Component?.ProdusentPostnr,
            ProdusentSted = automatikk?.Component?.ProdusentSted,
            ProdusentLand = automatikk?.Component?.ProdusentLand,
            ProdusentOrgnr = automatikk?.Component?.ProdusentOrgnr,
            ItemNavn = automatikk?.Component?.Navn,
            Antall = automatikk?.Antall,
            BreddeMm = dor.Bredde,
            HoydeMm = dor.Hoyde,
            Brannklasse = dor.Brann,
            FriBredde086 = dor.FriBredde086,
            Dorkonstruksjon = dor.DorIdMal?.Dorkonstruksjon,
            Karmkonstruksjon = dor.DorIdMal?.Karmkonstruksjon,
            GlassIDor = dor.DorIdMal?.GlassIDor switch { true => "Med glass", false => "Uten glass", null => null }
        };
    }

    // Ny CE for døra fra montørappen: første gang, eller årlig kontroll. Ved
    // årlig kontroll tas bruksområde og serienummer med fra forrige godkjenning,
    // resten gjøres på nytt.
    public static async Task<CeGodkjenning?> OpprettForDorAsync(ApplicationDbContext db, int dorId, int? brukerId)
    {
        var dor = await db.Dorer
            .Include(d => d.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(d => d.DorIdMal)
            .Include(d => d.Komponenter).ThenInclude(k => k.Component).ThenInclude(c => c!.Type)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == dorId);
        if (dor is null)
        {
            return null;
        }

        var ce = ForDor(dor, await NesteNummerAsync(db), brukerId);
        var forrige = await db.CeGodkjenninger.AsNoTracking().Where(c => c.DorId == dorId)
            .OrderByDescending(c => c.OpprettetDato).ThenByDescending(c => c.Id).FirstOrDefaultAsync();
        if (forrige is not null)
        {
            ce.Bruksomrade = forrige.Bruksomrade;
            ce.Serienummer = forrige.Serienummer;
            ce.ProduksjonsAar = forrige.ProduksjonsAar;
        }

        db.CeGodkjenninger.Add(ce);
        await db.SaveChangesAsync();
        return ce;
    }

    // Tilfeldig kode i QR-en på CE-merket. Den er ikke gjettbar, så statussiden
    // kan vises uten innlogging, og den er ny for hver godkjenning (hvert år).
    public static string NyttMerkeToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
