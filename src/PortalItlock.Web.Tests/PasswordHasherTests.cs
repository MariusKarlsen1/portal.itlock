using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Regresjonstester for passordhashing og -verifisering, inkludert
// DummyHash-mekanismen som ble lagt til for å fjerne en tidsbasert
// sidekanal på innlogging (se sikkerhetsgjennomgangen 2026-10-08,
// SECURITY_AUDIT.md funn 9).
public class PasswordHasherTests
{
    [Fact]
    public void Hash_ProdusererUlikeHasherForSammePassord()
    {
        // Tilfeldig salt per kall - to hasher av samme passord skal aldri bli like.
        var hash1 = PasswordHasher.Hash("mittHemmeligePassord123");
        var hash2 = PasswordHasher.Hash("mittHemmeligePassord123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_RiktigPassord_GirTrue()
    {
        var hash = PasswordHasher.Hash("korrektPassord123");

        Assert.True(PasswordHasher.Verify("korrektPassord123", hash));
    }

    [Fact]
    public void Verify_FeilPassord_GirFalse()
    {
        var hash = PasswordHasher.Hash("korrektPassord123");

        Assert.False(PasswordHasher.Verify("feilPassord456", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ikke-et-gyldig-format")]
    [InlineData("kun.ettpunktum.for.mye")]
    public void Verify_UgyldigLagretFormat_GirFalseIStedetForUnntak(string lagretVerdi)
    {
        // Verify skal aldri kaste et unntak på korrupt/uventet lagret
        // verdi - bare rapportere "ikke gyldig passord".
        var exception = Record.Exception(() => PasswordHasher.Verify("hvilketsomhelst", lagretVerdi));

        Assert.Null(exception);
        Assert.False(PasswordHasher.Verify("hvilketsomhelst", lagretVerdi));
    }

    [Fact]
    public void DummyHash_ErGyldigFormatSomAldriMatcherEtEktePassord()
    {
        // DummyHash brukes til å kjøre en PBKDF2-sjekk med samme kostnad
        // som en ekte verifisering når ingen bruker finnes (se Program.cs,
        // /account/login) - må derfor ha gyldig salt.hash-format, men skal
        // aldri faktisk matche noe reelt passord et menneske kunne skrevet.
        Assert.False(PasswordHasher.Verify("admin", PasswordHasher.DummyHash));
        Assert.False(PasswordHasher.Verify("passord123", PasswordHasher.DummyHash));
        Assert.False(PasswordHasher.Verify("", PasswordHasher.DummyHash));
    }

    [Fact]
    public void DummyHash_ErStabilInnenforSammeProsess()
    {
        // Samme verdi gjenbrukes gjennom hele prosessens levetid (static
        // readonly) - ikke generert på nytt for hvert innloggingsforsøk.
        Assert.Equal(PasswordHasher.DummyHash, PasswordHasher.DummyHash);
    }
}
