using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PortalItlock.Web.Services;

// Krypterer backupfiler med AES-256-GCM i biter på 1 MB, så store databaser
// ikke må ligge i minnet. Format:
//   "FKB1" | 8 byte tilfeldig prefiks | biter: [flagg 1][lengde 4][kryptert][tag 16]
// Nonce for bit i = prefiks + i (4 byte). Bitnummer og «siste bit»-flagget er
// med i autentiseringen, så biter ikke kan byttes om, fjernes eller kuttes.
public static class BackupKryptering
{
    private static readonly byte[] Magi = "FKB1"u8.ToArray();
    private const int BitStorrelse = 1024 * 1024;
    private const int TagStorrelse = 16;

    public static async Task KrypterAsync(Stream inn, Stream ut, byte[] nokkel, CancellationToken ct = default)
    {
        using var aes = new AesGcm(nokkel, TagStorrelse);
        var prefiks = RandomNumberGenerator.GetBytes(8);
        await ut.WriteAsync(Magi, ct);
        await ut.WriteAsync(prefiks, ct);

        var navarende = new byte[BitStorrelse];
        var neste = new byte[BitStorrelse];
        var lengde = await LesFullAsync(inn, navarende, ct);
        uint nr = 0;
        while (true)
        {
            var nesteLengde = lengde == BitStorrelse ? await LesFullAsync(inn, neste, ct) : 0;
            var erSiste = nesteLengde == 0;

            var kryptert = new byte[lengde];
            var tag = new byte[TagStorrelse];
            aes.Encrypt(Nonce(prefiks, nr), navarende.AsSpan(0, lengde), kryptert, tag, Aad(nr, erSiste));

            var hode = new byte[5];
            hode[0] = erSiste ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteInt32BigEndian(hode.AsSpan(1), lengde);
            await ut.WriteAsync(hode, ct);
            await ut.WriteAsync(kryptert, ct);
            await ut.WriteAsync(tag, ct);

            if (erSiste)
            {
                break;
            }

            (navarende, neste) = (neste, navarende);
            lengde = nesteLengde;
            nr++;
        }
    }

    public static async Task DekrypterAsync(Stream inn, Stream ut, byte[] nokkel, CancellationToken ct = default)
    {
        var magi = new byte[4];
        if (await LesFullAsync(inn, magi, ct) != 4 || !magi.AsSpan().SequenceEqual(Magi))
        {
            throw new InvalidDataException("Filen er ikke en Full Kontroll-backup.");
        }

        var prefiks = new byte[8];
        if (await LesFullAsync(inn, prefiks, ct) != 8)
        {
            throw new InvalidDataException("Backupfilen er ufullstendig.");
        }

        using var aes = new AesGcm(nokkel, TagStorrelse);
        var hode = new byte[5];
        var tag = new byte[TagStorrelse];
        uint nr = 0;
        while (true)
        {
            if (await LesFullAsync(inn, hode, ct) != 5)
            {
                throw new InvalidDataException("Backupfilen er kuttet.");
            }

            var erSiste = hode[0] == 1;
            var lengde = BinaryPrimitives.ReadInt32BigEndian(hode.AsSpan(1));
            if (lengde is < 0 or > BitStorrelse)
            {
                throw new InvalidDataException("Backupfilen er skadet.");
            }

            var kryptert = new byte[lengde];
            if (await LesFullAsync(inn, kryptert, ct) != lengde || await LesFullAsync(inn, tag, ct) != TagStorrelse)
            {
                throw new InvalidDataException("Backupfilen er kuttet.");
            }

            var klar = new byte[lengde];
            try
            {
                aes.Decrypt(Nonce(prefiks, nr), kryptert, tag, klar, Aad(nr, erSiste));
            }
            catch (AuthenticationTagMismatchException)
            {
                throw new InvalidDataException("Feil krypteringsnøkkel, eller filen er endret.");
            }

            await ut.WriteAsync(klar, ct);
            if (erSiste)
            {
                return;
            }

            nr++;
        }
    }

    private static byte[] Nonce(byte[] prefiks, uint nr)
    {
        var nonce = new byte[12];
        prefiks.CopyTo(nonce, 0);
        BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(8), nr);
        return nonce;
    }

    private static byte[] Aad(uint nr, bool erSiste)
    {
        var aad = new byte[5];
        BinaryPrimitives.WriteUInt32BigEndian(aad, nr);
        aad[4] = erSiste ? (byte)1 : (byte)0;
        return aad;
    }

    private static async Task<int> LesFullAsync(Stream s, byte[] buffer, CancellationToken ct)
    {
        var lest = 0;
        while (lest < buffer.Length)
        {
            var n = await s.ReadAsync(buffer.AsMemory(lest), ct);
            if (n == 0)
            {
                break;
            }

            lest += n;
        }

        return lest;
    }
}
