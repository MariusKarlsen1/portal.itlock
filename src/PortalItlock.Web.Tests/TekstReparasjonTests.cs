using PortalItlock.Web.Services;
using Xunit;

namespace PortalItlock.Web.Tests;

public class TekstReparasjonTests
{
    [Theory]
    [InlineData("A22-003-01 T├©mrerplan 3. etasje, bygg A", "A22-003-01 Tømrerplan 3. etasje, bygg A")]
    [InlineData("TÃ¸mrerplan", "Tømrerplan")]
    [InlineData("Tømrerplan", "Tømrerplan")]
    [InlineData("Plan 1 ├ vanlig", "Plan 1 ├ vanlig")]
    public void Fiks_retter_feil_tegnsett(string inn, string ut) => Assert.Equal(ut, TekstReparasjon.Fiks(inn));
}
