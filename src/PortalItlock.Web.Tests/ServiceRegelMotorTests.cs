using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Prisreglene for serviceavtaler: riktig kategori ut fra servicetypene på døren,
// ingen dobbel telling ved overlappende regler, konflikt ved lik prioritet,
// "Ikke kategorisert" når ingen regel treffer, og prisberegning med tillegg.
public class ServiceRegelMotorTests
{
    private static readonly Dictionary<int, string?> TypeKart = new()
    {
        [1] = "Dørautomatikk",
        [2] = "Elektrisk sluttstykke",
        [3] = "Motorlås",
        [4] = "Mekanisk lås",
        [5] = "Kortleser",
        [6] = null, // f.eks. "Sluttstykke 1" uten servicetype
    };

    private static Dor Dor(params int[] typer) => new()
    {
        Dornummer = "D-1",
        Komponenter = typer.Select(t => new DorKomponent { Component = new Component { Navn = "x", ComponentTypeId = t } }).ToList()
    };

    private static ServicePrisRegelBetingelse Har(string t) => new() { Verdi = t };
    private static ServicePrisRegelBetingelse IkkeHar(string t) => new() { Verdi = t, ErIkke = true };

    private static List<ServicePrisRegel> Regler() =>
    [
        new() { Id = 1, Navn = "Automatikk + motorlås", Prioritet = 1, Pris = 1100, Betingelser = [Har("Dørautomatikk"), Har("Motorlås")] },
        new() { Id = 2, Navn = "Automatikk + el. sluttstykke", Prioritet = 2, Pris = 950, Betingelser = [Har("Dørautomatikk"), Har("Elektrisk sluttstykke")] },
        new() { Id = 3, Navn = "Kun automatikk", Prioritet = 3, Pris = 800, Betingelser = [Har("Dørautomatikk")] },
        new() { Id = 4, Navn = "Kun el. sluttstykke", Prioritet = 5, Pris = 400, Betingelser = [Har("Elektrisk sluttstykke"), IkkeHar("Dørautomatikk"), IkkeHar("Motorlås")] },
        new() { Id = 5, Navn = "Mekanisk", Prioritet = 9, Pris = 250, KreverAlle = false, Betingelser = [Har("Mekanisk lås"), IkkeHar("Dørautomatikk"), IkkeHar("Elektrisk sluttstykke"), IkkeHar("Motorlås")] },
    ];

    [Fact]
    public void AutomatikkOgElektriskSluttstykke_GirRiktigRegel()
    {
        var r = ServiceRegelMotor.Klassifiser(Dor(1, 2, 4), Regler(), TypeKart);
        Assert.Equal("Automatikk + el. sluttstykke", r.Kategori);
        Assert.False(r.HarKonflikt);
    }

    [Fact]
    public void OverlappendeRegler_DorenFarBareEnKategori()
    {
        // Treffer både "Automatikk + el. sluttstykke" og "Kun automatikk" - den mest spesifikke vinner.
        var r = ServiceRegelMotor.Klassifiser(Dor(1, 2), Regler(), TypeKart);
        Assert.Equal(2, r.Treff.Count);
        Assert.Equal(2, r.Regel!.Id);
    }

    [Fact]
    public void IngenRegelTreffer_ErIkkeKategorisert()
    {
        var r = ServiceRegelMotor.Klassifiser(Dor(6), Regler(), TypeKart);
        Assert.Null(r.Regel);
        Assert.Equal(ServiceRegelMotor.IkkeKategorisert, r.Kategori);
    }

    [Fact]
    public void Ekskludering_HindrerTreff()
    {
        var r = ServiceRegelMotor.Klassifiser(Dor(2, 3), Regler(), TypeKart);
        Assert.NotEqual("Kun el. sluttstykke", r.Kategori);
        Assert.Null(r.Regel);
    }

    [Fact]
    public void LikPrioritet_GirKonfliktMenEnKategori()
    {
        var regler = Regler();
        regler.Add(new() { Id = 6, Navn = "Kortleserdør", Prioritet = 5, Pris = 500, Betingelser = [Har("Kortleser")] });
        var r = ServiceRegelMotor.Klassifiser(Dor(2, 5), regler, TypeKart);
        Assert.True(r.HarKonflikt);
        Assert.Equal(4, r.Regel!.Id);
    }

    [Fact]
    public void EndretKomponent_GirNyKategori()
    {
        var mekanisk = ServiceRegelMotor.Klassifiser(Dor(4), Regler(), TypeKart);
        var medAutomatikk = ServiceRegelMotor.Klassifiser(Dor(4, 1), Regler(), TypeKart);
        Assert.Equal("Mekanisk", mekanisk.Kategori);
        Assert.Equal("Kun automatikk", medAutomatikk.Kategori);
    }

    [Fact]
    public void InaktivRegel_Ignoreres()
    {
        var regler = Regler();
        regler[1].Aktiv = false;
        Assert.Equal("Kun automatikk", ServiceRegelMotor.Klassifiser(Dor(1, 2), regler, TypeKart).Kategori);
    }

    [Fact]
    public void Funksjon_KanBrukesSomBetingelse()
    {
        var regel = new ServicePrisRegel { Navn = "Rømning", Prioritet = 1, Betingelser = [new() { Felt = ServiceRegelFelt.Funksjon, Verdi = "rømning" }] };
        var d = new Dor { Dornummer = "D-2", Funksjoner = [new DorFunksjon { Navn = "Rømningsdør EN 179" }] };
        Assert.Equal("Rømning", ServiceRegelMotor.Klassifiser(d, [regel], TypeKart).Kategori);
    }

    [Theory]
    [InlineData("Dørautomatikk arm/skinne", "Dørautomatikk")]
    [InlineData("Kortleser inn", "Kortleser")]
    [InlineData("Panikkbeslag/Skåte", "Panikkbeslag")]
    [InlineData("Låskasse 1", "Mekanisk lås")]
    [InlineData("Sylinderskruer", null)]
    [InlineData("Sluttstykke 1", null)]
    public void StandardServicetype_FraNavn(string navn, string? forventet) =>
        Assert.Equal(forventet, ServiceRegelMotor.StandardServicetype(navn));

    [Fact]
    public void Beregn_LeggerPaTilleggMinsteprisOgRabatt()
    {
        var p = new Prosjekt { Navn = "A", ServiceGangerPerAr = 2, ServiceOppmote = 650, ServiceReise = 150 };
        var b = ServiceRegelMotor.Beregn(p, 7600, 120, []);
        Assert.Equal(8400, b.PerRunde);
        Assert.Equal(16800, b.Arspris);

        p.ServiceMinstepris = 10000;
        Assert.Equal(10000, ServiceRegelMotor.Beregn(p, 7600, 120, []).PerRunde);

        p.ServiceMinstepris = null;
        p.ServiceRabattProsent = 10;
        Assert.Equal(7560, ServiceRegelMotor.Beregn(p, 7600, 120, []).PerRunde);
    }

    [Fact]
    public void Grunnlag_LagresOgLesesTilbake()
    {
        var json = ServiceRegelMotor.LagGrunnlag([(5, "Mekanisk"), (7, "Kun automatikk")]);
        var les = ServiceRegelMotor.LesGrunnlag(json);
        Assert.Equal("Kun automatikk", les[7]);
        Assert.Empty(ServiceRegelMotor.LesGrunnlag("ikke json"));
    }
    [Fact]
    public void BareDisse_gjelder_bare_dorer_uten_andre_beslag()
    {
        var mekanisk = new ServicePrisRegel { Navn = "Mekanisk", BareDisse = true, Betingelser = [Har("Sylinder"), Har("Dørvrider"), Har("Skilt")] };
        HashSet<string> T(params string[] t) => new(t, StringComparer.OrdinalIgnoreCase);
        Assert.True(ServiceRegelMotor.Treffer(mekanisk, T("Sylinder"), []));
        Assert.True(ServiceRegelMotor.Treffer(mekanisk, T("Sylinder", "Dørvrider", "Skilt"), []));
        Assert.False(ServiceRegelMotor.Treffer(mekanisk, T("Sylinder", "Dørvrider", "Skilt", "Dørautomatikk"), []));
        Assert.False(ServiceRegelMotor.Treffer(mekanisk, T(), []));
    }

    [Fact]
    public void Kravgrupper_og_mellom_radene_en_av_i_raden()
    {
        // Automatikk m/adgangskontroll: automatikk OG (kortleser ELLER sentral).
        var r = new ServicePrisRegel
        {
            Navn = "Automatikk m/adgangskontroll", KreverAlle = true,
            Betingelser = [new() { Verdi = "Dørautomatikk", Gruppe = 1 }, new() { Verdi = "Kortleser", Gruppe = 2 }, new() { Verdi = "Sentral", Gruppe = 2 }]
        };
        HashSet<string> M(params string[] t) => new(t, StringComparer.OrdinalIgnoreCase);
        Assert.True(ServiceRegelMotor.Treffer(r, M("Dørautomatikk", "Kortleser"), []));
        Assert.True(ServiceRegelMotor.Treffer(r, M("Dørautomatikk", "Sentral", "Sylinder"), []));
        Assert.False(ServiceRegelMotor.Treffer(r, M("Dørautomatikk", "Elektrisk sluttstykke"), []));
        Assert.False(ServiceRegelMotor.Treffer(r, M("Kortleser", "Sentral"), []));
    }

    [Fact]
    public void Nye_servicetyper_fra_komponentnavn()
    {
        Assert.Equal("Skyvedørsautomatikk", ServiceRegelMotor.StandardServicetype("Skyvedørsautomatikk Besam"));
        Assert.Equal("Koordinator", ServiceRegelMotor.StandardServicetype("Koordinator"));
        Assert.Equal("Sentral", ServiceRegelMotor.StandardServicetype("ARX sentral"));
        Assert.Equal("Dørautomatikk", ServiceRegelMotor.StandardServicetype("Dørautomatikk slagdør"));
    }
}
