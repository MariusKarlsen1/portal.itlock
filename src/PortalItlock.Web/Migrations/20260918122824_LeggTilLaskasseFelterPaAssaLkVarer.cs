using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilLaskasseFelterPaAssaLkVarer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Engangs-backfill (etter ønske fra Marius) for alle aktive Assa
            // Abloy-varer som starter på "Lk" - setter komponenttype,
            // standard tidsbruk og produktgruppe likt malen han allerede
            // hadde satt manuelt på Lk1362S/28 L, og kobler på felles FDV.
            migrationBuilder.Sql(
                """
                UPDATE Components
                SET ComponentTypeId = (SELECT Id FROM ComponentTypes WHERE Navn = 'Låskasse 1'),
                    MontasjeMinutterArbeidsordre = 15,
                    MontasjeMinutterService = 2,
                    MontasjeMinutterProsjekt = 10
                WHERE Aktiv = 1
                  AND Leverandor = 'Assa Abloy'
                  AND Navn LIKE 'LK%'
                  AND EXISTS (SELECT 1 FROM ComponentTypes WHERE Navn = 'Låskasse 1');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO ComponentProduktgrupper (KomponenterId, ProduktgrupperId)
                SELECT c.Id, (SELECT Id FROM Produktgrupper WHERE Navn = 'Låskasser')
                FROM Components c
                WHERE c.Aktiv = 1
                  AND c.Leverandor = 'Assa Abloy'
                  AND c.Navn LIKE 'LK%'
                  AND EXISTS (SELECT 1 FROM Produktgrupper WHERE Navn = 'Låskasser')
                  AND NOT EXISTS (
                      SELECT 1 FROM ComponentProduktgrupper cp
                      WHERE cp.KomponenterId = c.Id
                        AND cp.ProduktgrupperId = (SELECT Id FROM Produktgrupper WHERE Navn = 'Låskasser')
                  );
                """);

            // FDV-en er felles for alle disse varene - lastes fra den
            // versjonerte PDF-en i Data/Seed og skrives kun til varer som
            // ikke allerede har et eget FDV-dokument (rører aldri
            // eksisterende data, se Prisimport-regelen om partial update).
            var pdfPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "FDV-MekaniskeLaaskasserAssa.pdf");
            if (File.Exists(pdfPath))
            {
                var hex = Convert.ToHexString(File.ReadAllBytes(pdfPath));
                migrationBuilder.Sql(
                    $"""
                    UPDATE Components
                    SET FdvData = X'{hex}',
                        FdvFilnavn = 'FDV - Mekaniske Låskasser ASSA.pdf',
                        FdvContentType = 'application/pdf'
                    WHERE FdvData IS NULL
                      AND Aktiv = 1
                      AND Leverandor = 'Assa Abloy'
                      AND Navn LIKE 'LK%';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
