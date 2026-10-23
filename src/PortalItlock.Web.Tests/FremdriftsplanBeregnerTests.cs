using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Fremdriftsplanen skal regne fasene med de samme reglene som sjekklisten i
// økonomiarket, og timekosten som arket «Timekost».
public class FremdriftsplanBeregnerTests
{
    private static readonly FremdriftsplanBeregner.Frister Frister = new(
        new DateTime(2026, 8, 10), new DateTime(2026, 8, 21), new DateTime(2026, 9, 7), new DateTime(2026, 12, 15));

    [Fact]
    public void Faser_FolgerReglaneFraArket()
    {
        var faser = FremdriftsplanBeregner.Faser(Frister);

        Assert.Equal((new DateTime(2026, 8, 10), new DateTime(2026, 8, 19)), (faser[0].Fra, faser[0].Til)); // start → tilbudsfrist − 2
        Assert.Equal((new DateTime(2026, 8, 21), new DateTime(2026, 8, 31)), (faser[1].Fra, faser[1].Til)); // tilbudsfrist → montasje − 7
        Assert.Equal((new DateTime(2026, 9, 7), new DateTime(2026, 12, 8)), (faser[2].Fra, faser[2].Til));  // montasje → overlevering − 7
        Assert.Equal((new DateTime(2026, 12, 8), new DateTime(2026, 12, 15)), (faser[3].Fra, faser[3].Til)); // overlevering − 7 → overlevering
    }

    [Fact]
    public void Aktivitet_FlytterSegMedFristen()
    {
        var for_ = FremdriftsplanBeregner.AktivitetDatoer(FremdriftsplanBeregner.Faser(Frister)[2], .5m, 1m);
        var etter = FremdriftsplanBeregner.AktivitetDatoer(FremdriftsplanBeregner.Faser(Frister with { Overlevering = Frister.Overlevering.AddDays(28) })[2], .5m, 1m);

        Assert.True(etter.Fra > for_.Fra);
        Assert.Equal(for_.Til.AddDays(28), etter.Til);
    }

    [Fact]
    public void UkerMellom_StemmerOverNyttar()
    {
        // WEEKNUM i arket gir 1 − 43 = −42 her.
        Assert.Equal(12, FremdriftsplanBeregner.UkerMellom(new DateTime(2026, 10, 23), new DateTime(2027, 1, 15)));
        Assert.Equal(8, FremdriftsplanBeregner.UkerMellom(new DateTime(2026, 10, 23), new DateTime(2026, 12, 15)));
    }

    [Fact]
    public void Andel_TellerAvsluttetOgIkkeAktueltSomFerdig()
    {
        var andel = FremdriftsplanBeregner.Andel([PlanOppgaveStatus.Avsluttet, PlanOppgaveStatus.IkkeAktuelt, PlanOppgaveStatus.Pabegynt, PlanOppgaveStatus.Utfordring]);

        Assert.Equal(0.5m, andel);
    }

    [Fact]
    public void FristerFor_LeggerTilFristforlengelse()
    {
        var p = new Prosjekt { Navn = "Test", MontasjestartDato = new DateTime(2026, 9, 7), OverleveringsfristDato = new DateTime(2026, 12, 15) };

        Assert.Equal(new DateTime(2026, 12, 18), FremdriftsplanBeregner.FristerFor(p, 3)!.Overlevering);
        Assert.Null(FremdriftsplanBeregner.FristerFor(new Prosjekt { Navn = "Uten frister" }));
    }

    [Fact]
    public void Malen_HarAlleFaserOgOppgaver()
    {
        Assert.Equal([0, 1, 2, 3], FremdriftsplanBeregner.Mal.Select(a => a.Fase).Distinct().Order());
        Assert.All(FremdriftsplanBeregner.Mal, a => Assert.NotEmpty(a.Oppgaver));
        Assert.All(FremdriftsplanBeregner.Mal.SelectMany(a => a.Oppgaver), o => Assert.Contains(o.Rolle, FremdriftsplanBeregner.Roller));
    }

    [Fact]
    public void Timekost_SomArket()
    {
        // Montør i arket: 520 000 × 1,35 + 150 000 = 852 000 / 1600 t = 532,50 kr.
        var montor = new AnsattTimekost { Arslonn = 520000, SosialeProsent = 35, Overhead = 150000, FakturerbareTimer = 1600 };

        Assert.Equal(532.50m, montor.Timekost);
    }
}
