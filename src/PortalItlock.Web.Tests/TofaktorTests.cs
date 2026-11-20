using System.Text;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

public class TofaktorTests
{
    // RFC 6238, vedlegg B: nøkkel "12345678901234567890" (SHA1), 8 siffer → vi bruker de 6 siste.
    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void Kode_StemmerMedStandarden(long sekunder, string forventet)
    {
        var nokkel = Encoding.ASCII.GetBytes("12345678901234567890");
        Assert.Equal(forventet, Tofaktor.Kode(nokkel, sekunder / 30));
    }

    [Fact]
    public void Sjekk_GodtarKodenNaa_OgIkkeSammeStegToGanger()
    {
        var hemmelighet = Tofaktor.NyHemmelighet();
        var nokkel = FraBase32(hemmelighet);
        var kode = Tofaktor.Kode(nokkel, Tofaktor.NaaSteg());

        Assert.True(Tofaktor.Sjekk(hemmelighet, kode, null, out var brukt));
        Assert.False(Tofaktor.Sjekk(hemmelighet, kode, brukt, out _));
        Assert.False(Tofaktor.Sjekk(hemmelighet, "000000" == kode ? "111111" : "000000", null, out _));
    }

    [Fact]
    public void Reservekode_VirkerBareEnGang()
    {
        var (koder, lagret) = Tofaktor.NyeReservekoder();
        var bruker = new Bruker { Navn = "Test", Epost = "t@t.no", TofaktorReservekoder = lagret };

        Assert.Equal(8, koder.Count);
        Assert.True(Tofaktor.BrukReservekode(bruker, koder[3].ToLowerInvariant()));
        Assert.False(Tofaktor.BrukReservekode(bruker, koder[3]));
        Assert.Equal(7, Tofaktor.AntallReservekoder(bruker.TofaktorReservekoder));
    }

    private static byte[] FraBase32(string s)
    {
        const string tegn = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var ut = new List<byte>();
        int buffer = 0, biter = 0;
        foreach (var c in s)
        {
            buffer = (buffer << 5) | tegn.IndexOf(c);
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
