using System.Security.Cryptography;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Backupfilene må kunne dekrypteres til nøyaktig det som ble kryptert, og
// endrede eller kuttede filer skal avvises i stedet for å gi feil data.
public class BackupKrypteringTests
{
    private static async Task<byte[]> KrypterAsync(byte[] data, byte[] nokkel)
    {
        using var ut = new MemoryStream();
        await BackupKryptering.KrypterAsync(new MemoryStream(data), ut, nokkel);
        return ut.ToArray();
    }

    private static async Task<byte[]> DekrypterAsync(byte[] data, byte[] nokkel)
    {
        using var ut = new MemoryStream();
        await BackupKryptering.DekrypterAsync(new MemoryStream(data), ut, nokkel);
        return ut.ToArray();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(1024 * 1024)]
    [InlineData(3 * 1024 * 1024 + 17)]
    public async Task Gir_tilbake_samme_data(int storrelse)
    {
        var nokkel = RandomNumberGenerator.GetBytes(32);
        var data = RandomNumberGenerator.GetBytes(storrelse);

        var tilbake = await DekrypterAsync(await KrypterAsync(data, nokkel), nokkel);

        Assert.Equal(data, tilbake);
    }

    [Fact]
    public async Task Avviser_endret_fil()
    {
        var nokkel = RandomNumberGenerator.GetBytes(32);
        var kryptert = await KrypterAsync(RandomNumberGenerator.GetBytes(5000), nokkel);
        kryptert[100] ^= 0xFF;

        await Assert.ThrowsAsync<InvalidDataException>(() => DekrypterAsync(kryptert, nokkel));
    }

    [Fact]
    public async Task Avviser_kuttet_fil()
    {
        var nokkel = RandomNumberGenerator.GetBytes(32);
        var kryptert = await KrypterAsync(RandomNumberGenerator.GetBytes(2 * 1024 * 1024 + 5), nokkel);

        // Fjern den siste biten helt.
        var kuttet = kryptert[..^(5 + 5 + 16 + 5)];

        await Assert.ThrowsAsync<InvalidDataException>(() => DekrypterAsync(kuttet, nokkel));
    }

    [Fact]
    public async Task Avviser_feil_nokkel()
    {
        var kryptert = await KrypterAsync(RandomNumberGenerator.GetBytes(100), RandomNumberGenerator.GetBytes(32));

        await Assert.ThrowsAsync<InvalidDataException>(() => DekrypterAsync(kryptert, RandomNumberGenerator.GetBytes(32)));
    }
}
