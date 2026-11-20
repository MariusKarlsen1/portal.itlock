using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QRCoder;

namespace PortalItlock.Web.Services;

/// <summary>
/// Tofaktor-innlogging med autentiseringsapp (Google/Microsoft Authenticator, Authy …).
/// Åpen standard TOTP (RFC 6238): HMAC-SHA1, 30 sekunder, 6 siffer. Ingen ekstern
/// tjeneste og ingen kostnad – appen og portalen regner ut samme kode hver for seg.
/// </summary>
public static class Tofaktor
{
    public const string Utsteder = "Full Kontroll";
    private const int Steg = 30;
    private const string Base32Tegn = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    // Reservekoder uten tegn som er lette å forveksle (0/O, 1/I/L).
    private const string ReserveTegn = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    // Navn på informasjonskapslene i innloggingen.
    public const string VenterCookie = "fk_tofaktor";
    public const string HuskCookie = "fk_tofaktor_husk";

    public static string NyHemmelighet() => TilBase32(RandomNumberGenerator.GetBytes(20));

    /// Nøkkelen i grupper på fire, til manuelt oppsett i appen.
    public static string Formater(string base32) =>
        string.Join(" ", Enumerable.Range(0, (base32.Length + 3) / 4).Select(i => base32.Substring(i * 4, Math.Min(4, base32.Length - i * 4))));

    public static string OtpUri(string kontonavn, string base32) =>
        $"otpauth://totp/{Uri.EscapeDataString(Utsteder)}:{Uri.EscapeDataString(kontonavn)}?secret={base32}&issuer={Uri.EscapeDataString(Utsteder)}&algorithm=SHA1&digits=6&period={Steg}";

    public static string QrDataUri(string tekst)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(tekst, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(6);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }

    public static long NaaSteg() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Steg;

    /// Sjekker koden mot nå og ett steg før/etter (klokka på telefonen kan gå litt feil).
    /// Gir tidssteget som ble brukt, så det kan lagres og ikke brukes igjen.
    public static bool Sjekk(string base32, string? kode, long? sistBruktSteg, out long brukt)
    {
        brukt = 0;
        var siffer = new string((kode ?? "").Where(char.IsDigit).ToArray());
        if (siffer.Length != 6)
        {
            return false;
        }
        var nokkel = FraBase32(base32);
        var naa = NaaSteg();
        for (var d = -1; d <= 1; d++)
        {
            var steg = naa + d;
            if (sistBruktSteg is { } s && steg <= s)
            {
                continue;
            }
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Kode(nokkel, steg)), Encoding.ASCII.GetBytes(siffer)))
            {
                brukt = steg;
                return true;
            }
        }
        return false;
    }

    public static string Kode(byte[] nokkel, long steg)
    {
        var teller = BitConverter.GetBytes(steg);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(teller);
        }
        var hash = HMACSHA1.HashData(nokkel, teller);
        var o = hash[^1] & 0x0F;
        var tall = ((hash[o] & 0x7F) << 24) | (hash[o + 1] << 16) | (hash[o + 2] << 8) | hash[o + 3];
        return (tall % 1_000_000).ToString("D6");
    }

    /// Åtte nye reservekoder (vises én gang) og hashene som lagres på brukeren.
    public static (List<string> Koder, string Lagret) NyeReservekoder()
    {
        var koder = Enumerable.Range(0, 8).Select(_ =>
        {
            var t = new string(Enumerable.Range(0, 8).Select(_ => ReserveTegn[RandomNumberGenerator.GetInt32(ReserveTegn.Length)]).ToArray());
            return t[..4] + "-" + t[4..];
        }).ToList();
        return (koder, JsonSerializer.Serialize(koder.Select(k => PasswordHasher.Hash(Normaliser(k))).ToList()));
    }

    public static int AntallReservekoder(string? lagret) =>
        string.IsNullOrEmpty(lagret) ? 0 : (JsonSerializer.Deserialize<List<string>>(lagret) ?? []).Count;

    /// Bruker en reservekode: stemmer den, fjernes den fra lista (gjelder bare én gang).
    public static bool BrukReservekode(Bruker bruker, string? kode)
    {
        var ren = Normaliser(kode ?? "");
        if (ren.Length != 8 || string.IsNullOrEmpty(bruker.TofaktorReservekoder))
        {
            return false;
        }
        var hasher = JsonSerializer.Deserialize<List<string>>(bruker.TofaktorReservekoder) ?? [];
        for (var i = 0; i < hasher.Count; i++)
        {
            if (PasswordHasher.Verify(ren, hasher[i]))
            {
                hasher.RemoveAt(i);
                bruker.TofaktorReservekoder = JsonSerializer.Serialize(hasher);
                return true;
            }
        }
        return false;
    }

    private static string Normaliser(string kode) =>
        new string(kode.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// Slår av tofaktor (brukeren selv, eller admin som tilbakestiller).
    public static void SlaAv(Bruker bruker)
    {
        bruker.TofaktorHemmelighet = null;
        bruker.TofaktorPaDato = null;
        bruker.TofaktorReservekoder = null;
        bruker.TofaktorSisteSteg = null;
        bruker.TofaktorStempel = Guid.NewGuid().ToString("N");
    }

    public static async Task<HashSet<BrukerRolle>> KrevdeRollerAsync(ApplicationDbContext db)
    {
        var raa = await db.SikkerhetInnstillinger.AsNoTracking().Select(s => s.KrevTofaktorRoller).FirstOrDefaultAsync();
        return LesRoller(raa);
    }

    public static HashSet<BrukerRolle> LesRoller(string? raa) =>
        (raa ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(r => Enum.TryParse<BrukerRolle>(r, out var rolle) ? (BrukerRolle?)rolle : null)
            .Where(r => r is not null).Select(r => r!.Value).ToHashSet();

    private static string TilBase32(byte[] data)
    {
        var sb = new StringBuilder();
        int buffer = 0, biter = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            biter += 8;
            while (biter >= 5)
            {
                sb.Append(Base32Tegn[(buffer >> (biter - 5)) & 31]);
                biter -= 5;
            }
        }
        if (biter > 0)
        {
            sb.Append(Base32Tegn[(buffer << (5 - biter)) & 31]);
        }
        return sb.ToString();
    }

    private static byte[] FraBase32(string tekst)
    {
        var ren = tekst.Replace(" ", "").TrimEnd('=').ToUpperInvariant();
        var ut = new List<byte>();
        int buffer = 0, biter = 0;
        foreach (var c in ren)
        {
            var v = Base32Tegn.IndexOf(c);
            if (v < 0)
            {
                continue;
            }
            buffer = (buffer << 5) | v;
            biter += 5;
            if (biter >= 8)
            {
                ut.Add((byte)((buffer >> (biter - 8)) & 0xFF));
                biter -= 8;
            }
        }
        return [.. ut];
    }
}
