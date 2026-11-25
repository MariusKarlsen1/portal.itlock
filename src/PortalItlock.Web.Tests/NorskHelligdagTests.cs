using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Røde dager regnes ut for hvert år (påsken flytter seg), så kalenderne alltid er riktige.
public class NorskHelligdagTests
{
    [Theory]
    [InlineData(2026, 4, 5, "1. påskedag")]
    [InlineData(2026, 4, 3, "Langfredag")]
    [InlineData(2026, 5, 14, "Kristi himmelfart")]
    [InlineData(2026, 5, 25, "2. pinsedag")]
    [InlineData(2027, 3, 28, "1. påskedag")]
    [InlineData(2027, 3, 25, "Skjærtorsdag")]
    [InlineData(2028, 4, 16, "1. påskedag")]
    [InlineData(2030, 4, 21, "1. påskedag")]
    [InlineData(2027, 5, 17, "Grunnlovsdagen")]
    [InlineData(2027, 12, 26, "2. juledag")]
    [InlineData(2028, 1, 1, "Nyttårsdag")]
    public void RodeDager_FolgerAret(int ar, int maned, int dag, string navn) =>
        Assert.Equal(navn, NorskHelligdag.Navn(new DateTime(ar, maned, dag)));

    [Theory]
    [InlineData(2027, 3, 27, "Påskeaften")]
    [InlineData(2026, 12, 24, "Julaften")]
    [InlineData(2026, 12, 31, "Nyttårsaften")]
    public void Merkedager(int ar, int maned, int dag, string navn) =>
        Assert.Equal(navn, NorskHelligdag.Merkedag(new DateTime(ar, maned, dag)));

    [Fact]
    public void VanligDag_ErIkkeRod() => Assert.Null(NorskHelligdag.Navn(new DateTime(2026, 11, 25)));
}
