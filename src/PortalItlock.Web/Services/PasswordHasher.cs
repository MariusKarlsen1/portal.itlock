using System.Security.Cryptography;

namespace PortalItlock.Web.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    // Brukes til å kjøre en "dummy"-PBKDF2-verifisering med samme kostnad
    // som en ekte sjekk, når det ikke finnes noen ekte bruker/passord å
    // sjekke mot - slik at svartiden på innlogging ikke avslører om en
    // konto/organisasjon finnes (tidsbasert sidekanal, se
    // sikkerhetsgjennomgangen 2026-10-08). Verdien er vilkårlig og skal
    // aldri faktisk matche et ekte passord.
    public static readonly string DummyHash = Hash(Guid.NewGuid().ToString());

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
