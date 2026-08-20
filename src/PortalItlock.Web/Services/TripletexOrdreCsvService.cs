using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public class TripletexOrdreCsvService(ApplicationDbContext db)
{
    // itlock bruker 25% mva på alt (samme sats som TilbudPdfService), som i Tripletex' importformat
    // tilsvarer kode "3". Sjekk MVA-kodeoversikten i Tripletex hvis dette ikke stemmer for din bedrift.
    private const string MvaKode25Prosent = "3";

    private static readonly CultureInfo NorskKultur = CultureInfo.GetCultureInfo("nb-NO");

    public async Task<(byte[]? Data, string? Feil)> GenerateAsync(int tilbudId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(t => t.Linjer)
            .FirstOrDefaultAsync(t => t.Id == tilbudId);

        if (tilbud is null)
        {
            return (null, "Fant ikke tilbudet.");
        }

        var kunde = tilbud.Prosjekt?.Kunde;
        if (kunde is null)
        {
            return (null, "Tilbudet må være koblet til et prosjekt med kunde for å kunne eksporteres.");
        }

        if (string.IsNullOrWhiteSpace(kunde.TripletexKundenummer))
        {
            return (null, $"Kunden \"{kunde.Navn}\" mangler Tripletex kundenummer. Legg det inn under Kunder først.");
        }

        var linjer = tilbud.Linjer
            .Where(l => l.LevertAv == LevertAv.F)
            .OrderBy(l => l.Rekkefolge)
            .ToList();

        var minutter = linjer.Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
        var arbeidstidTimer = tilbud.EstimertTimerOverride ?? (minutter / 60m);
        var kalkulertMontasjekost = Math.Round(tilbud.Timepris * arbeidstidTimer, 2);
        var montasjekost = tilbud.Montasjekost ?? kalkulertMontasjekost;

        if (linjer.Count == 0 && montasjekost <= 0)
        {
            return (null, "Tilbudet har ingen varer eller montasjekost levert av itlock å eksportere.");
        }

        var sb = new StringBuilder();
        sb.Append(string.Join(';',
            "ORDER NO", "ORDER DATE", "DELIVERY DATE", "CUSTOMER NO", "CUSTOMER NAME", "CUSTOMER EMAIL", "CUSTOMER PHONE",
            "POSTAL ADDR - LINE 1", "POSTAL ADDR - POSTAL NO", "POSTAL ADDR - CITY",
            "ORDER LINE - DESCRIPTION", "ORDER LINE - UNIT PRICE", "ORDER LINE - COUNT", "ORDER LINE - VAT CODE"));
        sb.Append("\r\n");

        var orderNo = tilbud.Id.ToString(CultureInfo.InvariantCulture);
        var orderDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        // Normal leveringstid for itlock er 4-5 uker (jf. standard tilbudsforside) - brukes som forslag,
        // juster gjerne i Tripletex sitt importvindu før du bekrefter.
        var deliveryDate = DateTime.Today.AddDays(28).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var forsteRad = true;

        void SkrivRad(string beskrivelse, decimal enhetspris, int antall)
        {
            string[] felter = forsteRad
                ?
                [
                    orderNo, orderDate, deliveryDate, kunde.TripletexKundenummer!, kunde.Navn, kunde.Epost ?? "", kunde.Telefon ?? "",
                    kunde.Adresse ?? "", kunde.Postnr ?? "", kunde.Sted ?? "",
                    beskrivelse, enhetspris.ToString("0.##", NorskKultur), antall.ToString(CultureInfo.InvariantCulture), MvaKode25Prosent
                ]
                :
                [
                    orderNo, "", "", "", "", "", "",
                    "", "", "",
                    beskrivelse, enhetspris.ToString("0.##", NorskKultur), antall.ToString(CultureInfo.InvariantCulture), MvaKode25Prosent
                ];

            sb.Append(string.Join(';', felter.Select(CsvFelt)));
            sb.Append("\r\n");
            forsteRad = false;
        }

        foreach (var l in linjer)
        {
            SkrivRad(l.Navn, l.Utpris, l.Antall);
        }

        if (montasjekost > 0)
        {
            SkrivRad("Montasje", montasjekost, 1);
        }

        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var tekst = Encoding.UTF8.GetBytes(sb.ToString());
        return ([.. bom, .. tekst], null);
    }

    private static string CsvFelt(string verdi)
    {
        if (verdi.Contains(';') || verdi.Contains('"') || verdi.Contains('\n'))
        {
            return $"\"{verdi.Replace("\"", "\"\"")}\"";
        }

        return verdi;
    }
}
