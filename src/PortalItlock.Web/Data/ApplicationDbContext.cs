using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<RequirementDimension> RequirementDimensions => Set<RequirementDimension>();
    public DbSet<RequirementValue> RequirementValues => Set<RequirementValue>();
    public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageRequirement> PackageRequirements => Set<PackageRequirement>();
    public DbSet<PackageComponent> PackageComponents => Set<PackageComponent>();
    public DbSet<DoorEnvironmentDocument> DoorEnvironmentDocuments => Set<DoorEnvironmentDocument>();
    public DbSet<Befaring> Befaringer => Set<Befaring>();
    public DbSet<BefaringDorfelt> BefaringDorfelt => Set<BefaringDorfelt>();
    public DbSet<BefaringLassystem> BefaringLassystemer => Set<BefaringLassystem>();
    public DbSet<BefaringDorfeltBilde> BefaringDorfeltBilder => Set<BefaringDorfeltBilde>();
    public DbSet<BefaringPdf> BefaringPdfer => Set<BefaringPdf>();
    public DbSet<GuideSide> GuideSider => Set<GuideSide>();
    public DbSet<Nokkelsystem> Nokkelsystemer => Set<Nokkelsystem>();
    public DbSet<Rekvirent> Rekvirenter => Set<Rekvirent>();
    public DbSet<SystemVedlegg> SystemVedlegg => Set<SystemVedlegg>();
    public DbSet<NokkelKvittering> NokkelKvitteringer => Set<NokkelKvittering>();
    public DbSet<Prisoverslag> Prisoverslag => Set<Prisoverslag>();
    public DbSet<PrisoverslagLinje> PrisoverslagLinjer => Set<PrisoverslagLinje>();
    public DbSet<LasUtskifting> LasUtskiftinger => Set<LasUtskifting>();
    public DbSet<Prosjekt> Prosjekter => Set<Prosjekt>();
    public DbSet<ProsjektVedlegg> ProsjektVedlegg => Set<ProsjektVedlegg>();
    public DbSet<Plantegning> Plantegninger => Set<Plantegning>();
    public DbSet<Dor> Dorer => Set<Dor>();
    public DbSet<DorKomponent> DorKomponenter => Set<DorKomponent>();
    public DbSet<DorFunksjon> DorFunksjoner => Set<DorFunksjon>();
    public DbSet<Tilbud> Tilbud => Set<Tilbud>();
    public DbSet<TilbudLinje> TilbudLinjer => Set<TilbudLinje>();
    public DbSet<Bruker> Brukere => Set<Bruker>();
    public DbSet<Arbeidsordre> Arbeidsordre => Set<Arbeidsordre>();
    public DbSet<Timeregistrering> Timeregistreringer => Set<Timeregistrering>();
    public DbSet<Kunde> Kunder => Set<Kunde>();
    public DbSet<DorIdMal> DorIdMaler => Set<DorIdMal>();
    public DbSet<TilbudForside> TilbudForsider => Set<TilbudForside>();
    public DbSet<FdvVedlegg> FdvVedlegg => Set<FdvVedlegg>();
    public DbSet<PlukklisteLinje> PlukklisteLinjer => Set<PlukklisteLinje>();
    public DbSet<Nokkel> Nokler => Set<Nokkel>();
    public DbSet<NokkelSylinder> NokkelSylindere => Set<NokkelSylinder>();
    public DbSet<LasplanReserve> LasplanReserver => Set<LasplanReserve>();
    public DbSet<NokkelLasplanReserve> NokkelLasplanReserver => Set<NokkelLasplanReserve>();
    public DbSet<Servicehenvendelse> Servicehenvendelser => Set<Servicehenvendelse>();
    public DbSet<ServicehenvendelseBilde> ServicehenvendelseBilder => Set<ServicehenvendelseBilde>();
    public DbSet<SjekklisteMal> SjekklisteMaler => Set<SjekklisteMal>();
    public DbSet<Rabattgruppe> Rabattgrupper => Set<Rabattgruppe>();
    public DbSet<SjekklistePunkt> SjekklistePunkter => Set<SjekklistePunkt>();
    public DbSet<ArbeidsordreSjekkpunkt> ArbeidsordreSjekkpunkter => Set<ArbeidsordreSjekkpunkt>();
    public DbSet<PrisHistorikk> PrisHistorikk => Set<PrisHistorikk>();
    public DbSet<Avvik> Avvik => Set<Avvik>();
    public DbSet<Tilvalg> Tilvalg => Set<Tilvalg>();
    public DbSet<TilvalgAlternativ> TilvalgAlternativer => Set<TilvalgAlternativ>();
    public DbSet<TilvalgMal> TilvalgMaler => Set<TilvalgMal>();
    public DbSet<TilvalgMalAlternativ> TilvalgMalAlternativer => Set<TilvalgMalAlternativ>();
    public DbSet<DorMedia> DorMedia => Set<DorMedia>();
    public DbSet<PlanUtstyr> PlanUtstyr => Set<PlanUtstyr>();
    public DbSet<PlanForbindelse> PlanForbindelser => Set<PlanForbindelse>();
    public DbSet<Servicerunde> Servicerunder => Set<Servicerunde>();
    public DbSet<ServicerundeDel> ServicerundeDeler => Set<ServicerundeDel>();
    public DbSet<ServicerundeSjekklistepunkt> ServicerundeSjekklistepunkter => Set<ServicerundeSjekklistepunkt>();
    public DbSet<ServicerundeSjekkpunkt> ServicerundeSjekkpunkter => Set<ServicerundeSjekkpunkt>();
    public DbSet<ServicerundeMedia> ServicerundeMedia => Set<ServicerundeMedia>();
    public DbSet<ArbeidsordreMedia> ArbeidsordreMedia => Set<ArbeidsordreMedia>();
    public DbSet<SjekklistePdf> SjekklistePdfer => Set<SjekklistePdf>();
    public DbSet<KundeOppfolgingNotat> KundeOppfolgingNotater => Set<KundeOppfolgingNotat>();
    public DbSet<FravarSoknad> FravarSoknader => Set<FravarSoknad>();
    public DbSet<HjemmesideTekst> HjemmesideTekster => Set<HjemmesideTekst>();
    public DbSet<KoblingsKategori> KoblingsKategorier => Set<KoblingsKategori>();
    public DbSet<KoblingsSkjema> KoblingsSkjemaer => Set<KoblingsSkjema>();
    public DbSet<KoblingsSymbol> KoblingsSymboler => Set<KoblingsSymbol>();
    public DbSet<KoblingsStrek> KoblingsStreker => Set<KoblingsStrek>();
    public DbSet<KoblingsSymbolBibliotek> KoblingsSymbolBibliotek => Set<KoblingsSymbolBibliotek>();
    public DbSet<LagretPdf> LagredePdfer => Set<LagretPdf>();
    public DbSet<Driftsmelding> Driftsmeldinger => Set<Driftsmelding>();
    public DbSet<DriftsmeldingMedia> DriftsmeldingMedia => Set<DriftsmeldingMedia>();
    public DbSet<CeGodkjenning> CeGodkjenninger => Set<CeGodkjenning>();
    public DbSet<CeGodkjenningMedia> CeGodkjenningMedia => Set<CeGodkjenningMedia>();
    public DbSet<CeMaleGrenseverdier> CeMaleGrenseverdier => Set<CeMaleGrenseverdier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageRequirement>(entity =>
        {
            entity.HasKey(pr => new { pr.PackageId, pr.RequirementValueId });

            entity.HasOne(pr => pr.Package)
                .WithMany(p => p.Krav)
                .HasForeignKey(pr => pr.PackageId);

            entity.HasOne(pr => pr.RequirementValue)
                .WithMany(rv => rv.Pakker)
                .HasForeignKey(pr => pr.RequirementValueId);
        });

        modelBuilder.Entity<PackageComponent>(entity =>
        {
            entity.HasKey(pc => new { pc.PackageId, pc.ComponentId });

            entity.HasOne(pc => pc.Package)
                .WithMany(p => p.Komponenter)
                .HasForeignKey(pc => pc.PackageId);

            entity.HasOne(pc => pc.Component)
                .WithMany(c => c.Pakker)
                .HasForeignKey(pc => pc.ComponentId);
        });

        modelBuilder.Entity<DorKomponent>(entity =>
        {
            entity.HasKey(dk => new { dk.DorId, dk.ComponentId });

            entity.HasOne(dk => dk.Dor)
                .WithMany(d => d.Komponenter)
                .HasForeignKey(dk => dk.DorId);

            entity.HasOne(dk => dk.Component)
                .WithMany()
                .HasForeignKey(dk => dk.ComponentId);

            entity.HasOne(dk => dk.MontertAvBruker)
                .WithMany(b => b.MonterteKomponenter)
                .HasForeignKey(dk => dk.MontertAvBrukerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Dor>()
            .HasMany(d => d.Funksjoner)
            .WithMany(f => f.Dorer)
            .UsingEntity(j => j.ToTable("DorDorFunksjoner"));

        modelBuilder.Entity<RequirementValue>()
            .HasOne(rv => rv.Dimensjon)
            .WithMany(d => d.Verdier)
            .HasForeignKey(rv => rv.RequirementDimensionId);

        modelBuilder.Entity<Component>()
            .HasOne(c => c.Type)
            .WithMany(t => t.Komponenter)
            .HasForeignKey(c => c.ComponentTypeId)
            .IsRequired(false);

        modelBuilder.Entity<Arbeidsordre>()
            .HasOne(a => a.Prosjekt)
            .WithMany(p => p.Arbeidsordre)
            .HasForeignKey(a => a.ProsjektId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Arbeidsordre>()
            .HasOne(a => a.AnsvarligMontor)
            .WithMany(m => m.Arbeidsordre)
            .HasForeignKey(a => a.AnsvarligMontorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Arbeidsordre>()
            .HasOne(a => a.Tilbud)
            .WithMany()
            .HasForeignKey(a => a.TilbudId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Prosjekt>()
            .HasOne(p => p.Kunde)
            .WithMany(k => k.Prosjekter)
            .HasForeignKey(p => p.KundeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Dor>()
            .HasOne(d => d.DorIdMal)
            .WithMany(m => m.Dorer)
            .HasForeignKey(d => d.DorIdMalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DorIdMal>()
            .HasOne(m => m.Prosjekt)
            .WithMany(p => p.DorIdMaler)
            .HasForeignKey(m => m.ProsjektId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Prosjekt>()
            .HasMany(p => p.Medlemmer)
            .WithMany(b => b.Prosjekter)
            .UsingEntity(j => j.ToTable("ProsjektMedlemmer"));

        modelBuilder.Entity<Timeregistrering>()
            .HasOne(t => t.Arbeidsordre)
            .WithMany(a => a.Timeregistreringer)
            .HasForeignKey(t => t.ArbeidsordreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Timeregistrering>()
            .HasOne(t => t.Montor)
            .WithMany(m => m.Timeregistreringer)
            .HasForeignKey(t => t.MontorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Timeregistrering>()
            .HasOne(t => t.BehandletAvBruker)
            .WithMany()
            .HasForeignKey(t => t.BehandletAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PlukklisteLinje>()
            .HasIndex(p => new { p.ProsjektId, p.ComponentId })
            .IsUnique();

        modelBuilder.Entity<NokkelSylinder>(entity =>
        {
            entity.HasKey(ns => new { ns.NokkelId, ns.DorId, ns.ComponentId });

            entity.HasOne(ns => ns.Nokkel)
                .WithMany()
                .HasForeignKey(ns => ns.NokkelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LasplanReserve>()
            .HasOne(r => r.Prosjekt)
            .WithMany()
            .HasForeignKey(r => r.ProsjektId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NokkelLasplanReserve>(entity =>
        {
            entity.HasKey(nr => new { nr.NokkelId, nr.LasplanReserveId });

            entity.HasOne(nr => nr.Nokkel)
                .WithMany()
                .HasForeignKey(nr => nr.NokkelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(nr => nr.LasplanReserve)
                .WithMany()
                .HasForeignKey(nr => nr.LasplanReserveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bruker>()
            .HasOne(b => b.Kunde)
            .WithMany()
            .HasForeignKey(b => b.KundeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Servicehenvendelse>()
            .HasOne(s => s.Kunde)
            .WithMany()
            .HasForeignKey(s => s.KundeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServicehenvendelseBilde>()
            .HasOne(b => b.Servicehenvendelse)
            .WithMany(s => s.Bilder)
            .HasForeignKey(b => b.ServicehenvendelseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tilvalg>()
            .HasOne(t => t.Prosjekt)
            .WithMany()
            .HasForeignKey(t => t.ProsjektId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TilvalgAlternativ>()
            .HasOne(a => a.Tilvalg)
            .WithMany(t => t.Alternativer)
            .HasForeignKey(a => a.TilvalgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TilvalgMalAlternativ>()
            .HasOne(a => a.TilvalgMal)
            .WithMany(t => t.Alternativer)
            .HasForeignKey(a => a.TilvalgMalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SjekklistePunkt>()
            .HasOne(p => p.SjekklisteMal)
            .WithMany(m => m.Punkter)
            .HasForeignKey(p => p.SjekklisteMalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArbeidsordreSjekkpunkt>()
            .HasOne(p => p.Arbeidsordre)
            .WithMany(a => a.Sjekkpunkter)
            .HasForeignKey(p => p.ArbeidsordreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArbeidsordreSjekkpunkt>()
            .HasOne(p => p.FullfortAvBruker)
            .WithMany()
            .HasForeignKey(p => p.FullfortAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Dor>()
            .HasOne(d => d.MontertAvBruker)
            .WithMany()
            .HasForeignKey(d => d.MontertAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Dor>()
            .HasOne(d => d.OppdatertAvBruker)
            .WithMany()
            .HasForeignKey(d => d.OppdatertAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Component>()
            .HasOne(c => c.Rabattgruppe)
            .WithMany(r => r.Komponenter)
            .HasForeignKey(c => c.RabattgruppeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PrisHistorikk>()
            .HasOne(p => p.Component)
            .WithMany()
            .HasForeignKey(p => p.ComponentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Avvik>()
            .HasOne(a => a.Dor)
            .WithMany()
            .HasForeignKey(a => a.DorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Avvik>()
            .HasOne(a => a.OpprettetAvBruker)
            .WithMany()
            .HasForeignKey(a => a.OpprettetAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DorMedia>()
            .HasOne(m => m.Dor)
            .WithMany()
            .HasForeignKey(m => m.DorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Driftsmelding>()
            .HasOne(d => d.Dor)
            .WithMany()
            .HasForeignKey(d => d.DorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Driftsmelding>()
            .HasOne(d => d.InnmeldtAvBruker)
            .WithMany()
            .HasForeignKey(d => d.InnmeldtAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DriftsmeldingMedia>()
            .HasOne(m => m.Driftsmelding)
            .WithMany(d => d.Media)
            .HasForeignKey(m => m.DriftsmeldingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlanUtstyr>()
            .HasOne(p => p.Plantegning)
            .WithMany()
            .HasForeignKey(p => p.PlantegningId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlanForbindelse>()
            .HasOne(f => f.FraUtstyr)
            .WithMany()
            .HasForeignKey(f => f.FraUtstyrId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlanForbindelse>()
            .HasOne(f => f.TilUtstyr)
            .WithMany()
            .HasForeignKey(f => f.TilUtstyrId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Servicerunde>()
            .HasOne(s => s.Prosjekt)
            .WithMany()
            .HasForeignKey(s => s.ProsjektId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Servicerunde>()
            .HasOne(s => s.UtfortAvBruker)
            .WithMany()
            .HasForeignKey(s => s.UtfortAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServicerundeDel>()
            .HasOne(d => d.Servicerunde)
            .WithMany(s => s.Deler)
            .HasForeignKey(d => d.ServicerundeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServicerundeDel>()
            .HasOne(d => d.Dor)
            .WithMany()
            .HasForeignKey(d => d.DorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServicerundeSjekkpunkt>()
            .HasOne(p => p.Servicerunde)
            .WithMany(s => s.Sjekkpunkter)
            .HasForeignKey(p => p.ServicerundeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServicerundeSjekkpunkt>()
            .HasOne(p => p.FullfortAvBruker)
            .WithMany()
            .HasForeignKey(p => p.FullfortAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServicerundeMedia>()
            .HasOne(m => m.Servicerunde)
            .WithMany(s => s.Media)
            .HasForeignKey(m => m.ServicerundeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArbeidsordreMedia>()
            .HasOne(m => m.Arbeidsordre)
            .WithMany(a => a.Media)
            .HasForeignKey(m => m.ArbeidsordreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BefaringPdf>()
            .HasOne(p => p.Befaring)
            .WithMany()
            .HasForeignKey(p => p.BefaringId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SjekklistePdf>()
            .HasOne(p => p.Arbeidsordre)
            .WithMany()
            .HasForeignKey(p => p.ArbeidsordreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KundeOppfolgingNotat>()
            .HasOne(n => n.Kunde)
            .WithMany(k => k.OppfolgingNotater)
            .HasForeignKey(n => n.KundeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KundeOppfolgingNotat>()
            .HasOne(n => n.OpprettetAvBruker)
            .WithMany()
            .HasForeignKey(n => n.OpprettetAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FravarSoknad>()
            .HasOne(f => f.Bruker)
            .WithMany()
            .HasForeignKey(f => f.BrukerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FravarSoknad>()
            .HasOne(f => f.BehandletAvBruker)
            .WithMany()
            .HasForeignKey(f => f.BehandletAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Tilbud>()
            .HasOne(t => t.OpprinneligTilbud)
            .WithMany()
            .HasForeignKey(t => t.OpprinneligTilbudId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KoblingsSymbol>()
            .HasOne(s => s.KoblingsSkjema)
            .WithMany(k => k.Symboler)
            .HasForeignKey(s => s.KoblingsSkjemaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KoblingsStrek>()
            .HasOne(s => s.KoblingsSkjema)
            .WithMany(k => k.Streker)
            .HasForeignKey(s => s.KoblingsSkjemaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KoblingsSymbol>()
            .HasOne(s => s.SymbolBibliotek)
            .WithMany()
            .HasForeignKey(s => s.SymbolBibliotekId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KoblingsSkjema>()
            .HasOne(k => k.Prosjekt)
            .WithMany()
            .HasForeignKey(k => k.ProsjektId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KoblingsSkjema>()
            .HasOne(k => k.Kategori)
            .WithMany(k => k.Skjemaer)
            .HasForeignKey(k => k.KategoriId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LagretPdf>()
            .HasIndex(p => new { p.EntityType, p.EntityId });

        modelBuilder.Entity<CeGodkjenning>()
            .HasOne(c => c.Dor)
            .WithMany()
            .HasForeignKey(c => c.DorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CeGodkjenning>()
            .HasOne(c => c.OpprettetAvBruker)
            .WithMany()
            .HasForeignKey(c => c.OpprettetAvBrukerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CeGodkjenningMedia>()
            .HasOne(m => m.CeGodkjenning)
            .WithMany(c => c.Media)
            .HasForeignKey(m => m.CeGodkjenningId)
            .OnDelete(DeleteBehavior.Cascade);

        SeedReferenceData(modelBuilder);
    }

    // Kravdimensjoner, kravverdier og komponenttyper hentet fra itlocks egen
    // "Beslagspakker Funksjoner Dørpakker flytskjema.xlsx" (Tabell1) - dette er
    // den reelle kravmodellen og komponentlisten firmaet faktisk bruker.
    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequirementDimension>().HasData(
            new RequirementDimension { Id = 1, Navn = "Type dør", Rekkefolge = 1 },
            new RequirementDimension { Id = 2, Navn = "Hvilke bruk", Rekkefolge = 2 },
            new RequirementDimension { Id = 3, Navn = "FG", Rekkefolge = 3 },
            new RequirementDimension { Id = 4, Navn = "Risikoklasse", Rekkefolge = 4 },
            new RequirementDimension { Id = 5, Navn = "Lukkefunksjon", Rekkefolge = 5 },
            new RequirementDimension { Id = 6, Navn = "Antall fløyer", Rekkefolge = 6 },
            new RequirementDimension { Id = 7, Navn = "Type beslag", Rekkefolge = 7 },
            new RequirementDimension { Id = 8, Navn = "Tilbakerømning", Rekkefolge = 8 },
            new RequirementDimension { Id = 9, Navn = "Postsonesylinder", Rekkefolge = 9 }
        );

        modelBuilder.Entity<RequirementValue>().HasData(
            new RequirementValue { Id = 1, RequirementDimensionId = 1, Verdi = "Innerdør", Rekkefolge = 1 },
            new RequirementValue { Id = 2, RequirementDimensionId = 1, Verdi = "Ytterdør", Rekkefolge = 2 },

            new RequirementValue { Id = 3, RequirementDimensionId = 2, Verdi = "Låsbar", Rekkefolge = 1 },
            new RequirementValue { Id = 4, RequirementDimensionId = 2, Verdi = "Ikke låsbar", Rekkefolge = 2 },
            new RequirementValue { Id = 5, RequirementDimensionId = 2, Verdi = "Toalett", Rekkefolge = 3 },
            new RequirementValue { Id = 6, RequirementDimensionId = 2, Verdi = "Forberedt for kortleser", Rekkefolge = 4 },
            new RequirementValue { Id = 7, RequirementDimensionId = 2, Verdi = "Med kortleser", Rekkefolge = 5 },
            new RequirementValue { Id = 8, RequirementDimensionId = 2, Verdi = "Med dørbladmontert kortleser", Rekkefolge = 6 },
            new RequirementValue { Id = 9, RequirementDimensionId = 2, Verdi = "Kun rømning RK 1-4", Rekkefolge = 7 },
            new RequirementValue { Id = 10, RequirementDimensionId = 2, Verdi = "Kun rømning RK 1-6", Rekkefolge = 8 },

            new RequirementValue { Id = 11, RequirementDimensionId = 3, Verdi = "Ikke FG", Rekkefolge = 1 },
            new RequirementValue { Id = 12, RequirementDimensionId = 3, Verdi = "FG", Rekkefolge = 2 },

            new RequirementValue { Id = 13, RequirementDimensionId = 4, Verdi = "Ikke rømning", Rekkefolge = 1 },
            new RequirementValue { Id = 14, RequirementDimensionId = 4, Verdi = "1-4", Rekkefolge = 2 },
            new RequirementValue { Id = 15, RequirementDimensionId = 4, Verdi = "1-6", Rekkefolge = 3 },

            new RequirementValue { Id = 16, RequirementDimensionId = 5, Verdi = "Ingen lukker", Rekkefolge = 1 },
            new RequirementValue { Id = 17, RequirementDimensionId = 5, Verdi = "Dørlukker", Rekkefolge = 2 },
            new RequirementValue { Id = 18, RequirementDimensionId = 5, Verdi = "Automatikk", Rekkefolge = 3 },
            new RequirementValue { Id = 19, RequirementDimensionId = 5, Verdi = "Dørlukker/automatikk", Rekkefolge = 4 },

            new RequirementValue { Id = 20, RequirementDimensionId = 6, Verdi = "1-fløyet", Rekkefolge = 1 },
            new RequirementValue { Id = 21, RequirementDimensionId = 6, Verdi = "2-fløyet", Rekkefolge = 2 },
            new RequirementValue { Id = 22, RequirementDimensionId = 6, Verdi = "Skyvedør", Rekkefolge = 3 },

            new RequirementValue { Id = 23, RequirementDimensionId = 7, Verdi = "Mekanisk", Rekkefolge = 1 },
            new RequirementValue { Id = 24, RequirementDimensionId = 7, Verdi = "Elektrisk", Rekkefolge = 2 },
            new RequirementValue { Id = 25, RequirementDimensionId = 7, Verdi = "Lukket/låst signal", Rekkefolge = 3 },
            new RequirementValue { Id = 26, RequirementDimensionId = 7, Verdi = "Hengelås", Rekkefolge = 4 },

            new RequirementValue { Id = 27, RequirementDimensionId = 8, Verdi = "Nei", Rekkefolge = 1 },
            new RequirementValue { Id = 28, RequirementDimensionId = 8, Verdi = "Ja", Rekkefolge = 2 },
            new RequirementValue { Id = 29, RequirementDimensionId = 8, Verdi = "Ikke aktuelt", Rekkefolge = 3 },

            new RequirementValue { Id = 30, RequirementDimensionId = 9, Verdi = "Nei", Rekkefolge = 1 },
            new RequirementValue { Id = 31, RequirementDimensionId = 9, Verdi = "Ja", Rekkefolge = 2 }
        );

        modelBuilder.Entity<ComponentType>().HasData(
            new ComponentType { Id = 1, Navn = "Låskasse 1" },
            new ComponentType { Id = 2, Navn = "Sluttstykke 1" },
            new ComponentType { Id = 3, Navn = "Stolpe 1" },
            new ComponentType { Id = 4, Navn = "Sylinder 1 utv" },
            new ComponentType { Id = 5, Navn = "Sylinder 1 innv." },
            new ComponentType { Id = 6, Navn = "Sylinder utstyr" },
            new ComponentType { Id = 7, Navn = "Sylinder utstyr 2" },
            new ComponentType { Id = 8, Navn = "Sylinder utstyr 3" },
            new ComponentType { Id = 9, Navn = "Håndtak" },
            new ComponentType { Id = 10, Navn = "Dørvrider" },
            new ComponentType { Id = 11, Navn = "Skilt 1" },
            new ComponentType { Id = 12, Navn = "Låskasse 2" },
            new ComponentType { Id = 13, Navn = "Sluttstykke 2" },
            new ComponentType { Id = 14, Navn = "Sluttstykke 2 utstyr" },
            new ComponentType { Id = 15, Navn = "Sylinder 2 utv" },
            new ComponentType { Id = 16, Navn = "Sylinder 2 innv" },
            new ComponentType { Id = 17, Navn = "Sylinder 2 utstyr" },
            new ComponentType { Id = 18, Navn = "Sylinder 2 utstyr 2" },
            new ComponentType { Id = 19, Navn = "Skilt 2" },
            new ComponentType { Id = 20, Navn = "Sylinderskruer" },
            new ComponentType { Id = 21, Navn = "Dørautomatikk" },
            new ComponentType { Id = 22, Navn = "Dørautomatikk arm/skinne" },
            new ComponentType { Id = 23, Navn = "Klemsikring bakkant" },
            new ComponentType { Id = 24, Navn = "Klemsikring forkant" },
            new ComponentType { Id = 25, Navn = "Kelmsikring karmoverføring" },
            new ComponentType { Id = 26, Navn = "Dørautomatikk utstyr" },
            new ComponentType { Id = 27, Navn = "Dørautomatikk utstyr 2" },
            new ComponentType { Id = 28, Navn = "Dørautomatikk blindplugg" },
            new ComponentType { Id = 29, Navn = "Dørautomatikk utstyr 4" },
            new ComponentType { Id = 30, Navn = "Dørautomatikk utstyr 5" },
            new ComponentType { Id = 31, Navn = "Kortleser inn" },
            new ComponentType { Id = 32, Navn = "Kortleser ut" },
            new ComponentType { Id = 33, Navn = "Kortleser styreenhet" },
            new ComponentType { Id = 34, Navn = "Impulsbryter innv" },
            new ComponentType { Id = 35, Navn = "Impulsbryter utv." },
            new ComponentType { Id = 36, Navn = "Impulsbryter utstyr" },
            new ComponentType { Id = 37, Navn = "Nøkkelbryter" },
            new ComponentType { Id = 38, Navn = "Nkl.bryter sylinder" },
            new ComponentType { Id = 39, Navn = "Dørlukker aktiv fløy" },
            new ComponentType { Id = 40, Navn = "Dørlukker arm/skinne" },
            new ComponentType { Id = 41, Navn = "Dørlukker passiv fløy" },
            new ComponentType { Id = 42, Navn = "Dørlukker utstyr" },
            new ComponentType { Id = 43, Navn = "Dørlukker utstyr 2" },
            new ComponentType { Id = 44, Navn = "Panikkbeslag/Skåte" },
            new ComponentType { Id = 45, Navn = "Panikkbeslag utstyr" },
            new ComponentType { Id = 46, Navn = "Panikkbeslag utstyr 2" },
            new ComponentType { Id = 47, Navn = "Panikkbeslag utstyr 3" },
            new ComponentType { Id = 48, Navn = "Panikkbeslag utstyr 4" },
            new ComponentType { Id = 49, Navn = "Panikkbeslag utstyr 5" },
            new ComponentType { Id = 50, Navn = "Magnetlås passiv fløy" },
            new ComponentType { Id = 51, Navn = "Magnetlås utstyr" },
            new ComponentType { Id = 52, Navn = "Magnetlås utstyr 2" },
            new ComponentType { Id = 53, Navn = "Nødutstyr mekanisk" },
            new ComponentType { Id = 54, Navn = "Nødutstyr elektrisk" },
            new ComponentType { Id = 55, Navn = "Karmoverføring aktiv fløy" },
            new ComponentType { Id = 56, Navn = "Karmoverføring passiv fløy" },
            new ComponentType { Id = 57, Navn = "Kabel" },
            new ComponentType { Id = 58, Navn = "Dørstopper" },
            new ComponentType { Id = 59, Navn = "Magnetkontakt" },
            new ComponentType { Id = 60, Navn = "Grensesnittboks" },
            new ComponentType { Id = 61, Navn = "Grensesnittboks utstyr 1" },
            new ComponentType { Id = 62, Navn = "Grensesnittboks utstyr 2" },
            new ComponentType { Id = 63, Navn = "Diverse 1" },
            new ComponentType { Id = 64, Navn = "Diverse 2" },
            new ComponentType { Id = 65, Navn = "Diverse 3" }
        );

        modelBuilder.Entity<DoorEnvironmentDocument>().HasData(
            new DoorEnvironmentDocument { Id = 1, Navn = "Dørmiljø 1", FileName = "dormiljo-1.pdf", Rekkefolge = 1 },
            new DoorEnvironmentDocument { Id = 2, Navn = "Dørmiljø 2", FileName = "dormiljo-2.pdf", Rekkefolge = 2 },
            new DoorEnvironmentDocument { Id = 3, Navn = "Dørmiljø 3", FileName = "dormiljo-3.pdf", Rekkefolge = 3 },
            new DoorEnvironmentDocument { Id = 4, Navn = "Dørmiljø 4", FileName = "dormiljo-4.pdf", Rekkefolge = 4 },
            new DoorEnvironmentDocument { Id = 5, Navn = "Dørmiljø 5", FileName = "dormiljo-5.pdf", Rekkefolge = 5 },
            new DoorEnvironmentDocument { Id = 6, Navn = "Dørmiljø 6", FileName = "dormiljo-6.pdf", Rekkefolge = 6 },
            new DoorEnvironmentDocument { Id = 7, Navn = "Dørmiljø 7", FileName = "dormiljo-7.pdf", Rekkefolge = 7 },
            new DoorEnvironmentDocument { Id = 8, Navn = "Dørmiljø 8", FileName = "dormiljo-8.pdf", Rekkefolge = 8 },
            new DoorEnvironmentDocument { Id = 9, Navn = "Dørmiljø 9", FileName = "dormiljo-9.pdf", Rekkefolge = 9 }
        );

        modelBuilder.Entity<KoblingsKategori>().HasData(
            new KoblingsKategori { Id = 1, Navn = "ARX", Rekkefolge = 1 },
            new KoblingsKategori { Id = 2, Navn = "Salto", Rekkefolge = 2 },
            new KoblingsKategori { Id = 3, Navn = "Diverse", Rekkefolge = 3 }
        );

        modelBuilder.Entity<HjemmesideTekst>().HasData(
            new HjemmesideTekst
            {
                Id = 1,
                Tittel = "Itlock Full Kontroll",
                Ingress = "Internt verktøy for prosjektering, befaring og det daglige arbeidet med dørpakker, komponenter og krav. Velg hvem du er, så finner du raskt det du trenger.",
                MontorerTittel = "For montører",
                MontorerBeskrivelse = "Guide, befaring og vedlegg til bruk i felt",
                ProsjektlederTittel = "For prosjektledere",
                ProsjektlederBeskrivelse = "Systemer, prosjektering og prosjekter",
                AdminTittel = "For admin",
                AdminBeskrivelse = "Opprett og vedlikehold grunnlagsdata",
            }
        );

        modelBuilder.Entity<TilbudForside>().HasData(
            new TilbudForside
            {
                Id = 1,
                Navn = "Standard - Lås & beslag",
                Innhold =
"""
# Tilbudsforutsetninger og forbehold
Dette tilbudet er utarbeidet på bakgrunn av mottatt grunnlag fra byggherre/entrepenør/arkitekt og vedlagt beslagsliste.

Tilbudet inkluderer ikke HEPRO dørbladlesere til sengerommene da jeg regner med kommunen leverer dette.

Forutsetter at elektro medtar UPS hvis ikke kan dette leveres og vil da komme tillegg.

## Montering
Montasje, rigg, drift og kjøring er inkludert i tilbudet.

## Betalingsbetingelser
Netto pr. 30 dager.
Varer leveres og faktureres ved oppstart – eller iht. fremdriftsplan.
Montasje faktureres etter fremdrift – eller iht. kontraktens faktureringsbetingelser.

## Fremdrift og kommunikasjon
Gjeldende fremdriftsplan legges til grunn og anses som førende for fremdrift.
E-post regnes som skriftlig kommunikasjon dersom annet ikke er avtalt.

## Leveringstid
Normalt 4–5 uker.

## Tilbudets gyldighet
Tilbudet er gyldig i 30 dager fra tilbudsdato.

## Låssystem
Låssystem leveres med patenterte nøkler. Antall nøkler leveres iht. oppgitt mengde. Dersom antall ikke er oppgitt, avregnes dette etter at låsplan er godkjent.
Låsplan omfatter én (1) gangs utarbeidelse av produksjonsgrunnlag. Endringer etter produksjonsgrunnlag belastes etter medgått tid.
Endring til annet låssystem enn tilbudt kan medføre priskonsekvens.

## Endrings- og regningsarbeid
Endrings- og/eller regningsarbeid utføres kun ved skriftlig bestilling.

# Beslag

## Automatikker (slagdør)
For dører med slagdørautomatikk må det legges inn spikerslag/forsterkning i dør. Dører med overlysfelt må ha spikerslag minimum 100 mm over karm for montering av slagdørautomatikk.
Slagdørautomatikk forutsettes levert med standard arm/glideskinne på karmside. Spesialarm kan medføre tillegg og avregnes.
Døråpner skal monteres utenfor dørens slagradius, være godt synlig og plasseres med betjeningshøyde 0,8–1,1 m over gulv. Prosjekteres av RIE.
Dørbladbredde på dører med dørautomatikk må være minimum 750 mm. Ved doble dører der aktivt felt er større enn passivt, tas forbehold om at aktivt dørblad alene dekker nødvendig rømningsbredde.

## CE-godkjenning og servicekrav
Alle automatikker og tilhørende sikkerhetsutstyr er prosjektert i samsvar med gjeldende regelverk og krav i Maskindirektivet og NS-EN 16005.
Automatikker skal CE-godkjennes som del av installasjonen og skal minimum ha årlig service/vedlikehold utført av kompetent personell. Ved manglende service, bortfaller CE sertifisering og det kan påvirke funksjon, sikkerhet og garanti.

## Elektriske sluttstykker / motorlås / solenoidlås
Karm–dørblad klaring må være korrekt: 3 mm ± 2 mm.
Dørprodusent må hensynta listetrykk, utfresing og plassering av låskasse.
Dørprodusent må melde tilbake dersom andre stolper må benyttes for å tilfredsstille krav/godkjenninger.
Stolpe til el. sluttstykke er tilfeldig valgt – eventuell endring avregnes.
Dersom adgangskontroll benytter balansert tilbakemelding, kan det kreves sluttstykker med mikrobryter (STEP) – endring avregnes.

## Panikkbeslag
Dører med panikkbeslag må være utført slik at begge dørblader kan åpnes samtidig ved rømning – ansvar dørprodusent.
Montering forutsetter ferdig dørmiljø og gulv.

# Dørprodusent og elektro

## Dørleverandør medtar (Ld i beslagliste)
Hengsler, låskasser uten mikrobryter, mekaniske sluttstykker, innfelte skåter, karmoverføring med rørføring og trekkertråd ferdig montert, samt skyvedørsautomatikker komplett.
Alle FG-godkjente dører leveres med godkjent bakkantsikring.
Dørblad/karm skal være forboret/gjenget/forsterket for tilbudte produkter (F i beslagliste).
Dørprodusent må melde tilbake dersom prosjektert stolpe til elektrisk sluttstykke må endres.

## Elektroleverandør/RIE medtar (Le i beslagliste)
Alle beslag merket Le i beslagliste.
All kabling, rørføringer, bokser, strømforsyninger med batteribackup, tilkobling til brannvarslingsanlegg.
230V driftsspenning og sentral UPS medtas av elektro. Dersom ikke annet er presisert, leveres elektriske lås som 24VDC.
Det må være minimum 16 mm klaring mellom karm og veggutsparing for plass til kabler i dørmiljø.
Ferdige føringsveier forutsettes slik at synlig kabel ikke forekommer.

# Garanti

## Garantivilkår
Utstyr skal vedlikeholdes iht. instruks i FDV. Vedlikehold skal utføres av kompetent/opplært personell og kunne dokumenteres.
Dokumentasjon skal vise: hva, når og hvem. Manglende dokumentasjon/feil vedlikehold medfører bortfall av garanti og reklamasjonsrett.
"""
            }
        );
    }
}
