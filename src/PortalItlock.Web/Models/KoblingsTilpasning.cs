using System.Text.Json;

namespace PortalItlock.Web.Models;

// Retter eldre koblingsskjema der rammen rundt en komponent er høyere eller
// bredere enn selve bildet (navnet havnet langt under, og hjørnehåndtaket
// utenfor bildet). Rammen krympes til bildet uten at bildet flytter seg, egne
// koblingspunkter følger med, og strekender som satt i et av de vanlige
// punktene (midt på hver kant) flyttes inn til den nye kanten.
public static class KoblingsTilpasning
{
    // Lerretet er 16:13 (bredde:høyde), som i KoblingsSkjemaVisning.
    public const double LerretAspekt = 13.0 / 16.0;
    private const double Toleranse = 0.02;   // 2 % avvik regnes som riktig
    private const double PunktAvstand = 0.35; // hvor nær en strekende må være et punkt (i prosent)

    public sealed record Endring(KoblingsSymbol Symbol, double GammelX, double GammelY, double GammelB, double GammelH);

    // Retter alle symbolene og strekene i ett skjema. Gir true når noe ble endret.
    public static bool TilpassSkjema(IEnumerable<KoblingsSymbol> symboler, IEnumerable<KoblingsStrek> streker)
    {
        var endringer = symboler.Select(TilpassTilBilde).Where(e => e is not null).Select(e => e!).ToList();
        if (endringer.Count == 0)
        {
            return false;
        }

        foreach (var strek in streker)
        {
            var punkter = JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(strek.PunkterJson) ?? [];
            if (punkter.Count < 2)
            {
                continue;
            }
            var endret = false;
            foreach (var ende in new[] { 0, punkter.Count - 1 })
            {
                var nabo = ende == 0 ? 1 : punkter.Count - 2;
                foreach (var e in endringer)
                {
                    if (FlyttTilNyttPunkt(e, punkter[ende], punkter[nabo]))
                    {
                        endret = true;
                        break;
                    }
                }
            }
            if (endret)
            {
                strek.PunkterJson = JsonSerializer.Serialize(punkter);
            }
        }
        return true;
    }

    // Krymper rammen til bildet (bildet ligger midt i rammen med object-fit: contain).
    public static Endring? TilpassTilBilde(KoblingsSymbol s)
    {
        if (s.ElementType != KoblingsElementType.Komponent || s.Bredde <= 0 || s.Hoyde <= 0)
        {
            return null;
        }
        var k = KoblingsKomponentKatalog.Finn(s.KomponentNokkel);
        if (k?.Bilde is null)
        {
            return null;
        }

        var riktig = k.Forhold / LerretAspekt;   // høyde-% per bredde-%
        var naa = s.Hoyde / s.Bredde;
        if (Math.Abs(naa - riktig) / riktig < Toleranse)
        {
            return null;
        }

        var gammel = new Endring(s, s.PosX, s.PosY, s.Bredde, s.Hoyde);
        var (nyB, nyH) = naa > riktig ? (s.Bredde, s.Bredde * riktig) : (s.Hoyde / riktig, s.Hoyde);
        var dx = (s.Bredde - nyB) / 2;
        var dy = (s.Hoyde - nyH) / 2;

        var porter = KoblingsPorter.Les(s.PorterJson);
        if (porter.Count > 0)
        {
            s.PorterJson = KoblingsPorter.Skriv(porter
                .Select(p => (Math.Clamp((p.X * s.Bredde - dx) / nyB, 0, 1), Math.Clamp((p.Y * s.Hoyde - dy) / nyH, 0, 1)))
                .ToList());
        }

        s.PosX = Math.Round(s.PosX + dx, 3);
        s.PosY = Math.Round(s.PosY + dy, 3);
        s.Bredde = Math.Round(nyB, 3);
        s.Hoyde = Math.Round(nyH, 3);
        return gammel;
    }

    // Satt strekenden i et av de vanlige punktene (midt på en kant) på den gamle
    // rammen, flyttes den til samme punkt på den nye. Naboen følger med på samme
    // akse, så rette vinkler holdes.
    private static bool FlyttTilNyttPunkt(Endring e, KoblingsStrekPunkt ende, KoblingsStrekPunkt nabo)
    {
        var s = e.Symbol;
        (double X, double Y)[] gamle =
        [
            (e.GammelX + e.GammelB / 2, e.GammelY), (e.GammelX + e.GammelB, e.GammelY + e.GammelH / 2),
            (e.GammelX + e.GammelB / 2, e.GammelY + e.GammelH), (e.GammelX, e.GammelY + e.GammelH / 2)
        ];
        (double X, double Y)[] nye =
        [
            (s.PosX + s.Bredde / 2, s.PosY), (s.PosX + s.Bredde, s.PosY + s.Hoyde / 2),
            (s.PosX + s.Bredde / 2, s.PosY + s.Hoyde), (s.PosX, s.PosY + s.Hoyde / 2)
        ];
        for (var i = 0; i < 4; i++)
        {
            if (Math.Abs(ende.X - gamle[i].X) < PunktAvstand && Math.Abs(ende.Y - gamle[i].Y) < PunktAvstand)
            {
                if (Math.Abs(nabo.X - ende.X) < 0.01)
                {
                    nabo.X = nye[i].X;
                }
                if (Math.Abs(nabo.Y - ende.Y) < 0.01)
                {
                    nabo.Y = nye[i].Y;
                }
                ende.X = Math.Round(nye[i].X, 3);
                ende.Y = Math.Round(nye[i].Y, 3);
                return true;
            }
        }
        return false;
    }
}
