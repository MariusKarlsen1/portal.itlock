using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilSylinderskiltFelterPaAssaSk5968Varer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Engangs-backfill (etter ønske fra Marius) for alle aktive Assa
            // Abloy-varer med varenavn som starter på "Sk5968/" - setter
            // komponenttype, standard tidsbruk, enhet og produktgruppe, samt
            // felles FDV og overflate/varebilde utledet fra navnesuffikset
            // (Inn/Utv Fkr/Fkrm/Msm).
            migrationBuilder.Sql(
                """
                UPDATE Components
                SET ComponentTypeId = (SELECT Id FROM ComponentTypes WHERE Navn = 'Sylinderskilt'),
                    MontasjeMinutterArbeidsordre = 6,
                    MontasjeMinutterService = 1,
                    MontasjeMinutterProsjekt = 4,
                    Enhet = 'Stk'
                WHERE Aktiv = 1
                  AND Leverandor = 'Assa Abloy'
                  AND Navn LIKE 'SK5968/%'
                  AND EXISTS (SELECT 1 FROM ComponentTypes WHERE Navn = 'Sylinderskilt');
                """);

            migrationBuilder.Sql(
                """
                UPDATE Components SET Overflate = 'Fkr'
                WHERE Aktiv = 1 AND Leverandor = 'Assa Abloy' AND Navn LIKE 'SK5968/%' AND Navn LIKE '%Fkr';
                """);
            migrationBuilder.Sql(
                """
                UPDATE Components SET Overflate = 'Fkrm'
                WHERE Aktiv = 1 AND Leverandor = 'Assa Abloy' AND Navn LIKE 'SK5968/%' AND Navn LIKE '%Fkrm';
                """);
            migrationBuilder.Sql(
                """
                UPDATE Components SET Overflate = 'Msm'
                WHERE Aktiv = 1 AND Leverandor = 'Assa Abloy' AND Navn LIKE 'SK5968/%' AND Navn LIKE '%Msm';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO ComponentProduktgrupper (KomponenterId, ProduktgrupperId)
                SELECT c.Id, (SELECT Id FROM Produktgrupper WHERE Navn = 'Låser og låssylindere')
                FROM Components c
                WHERE c.Aktiv = 1
                  AND c.Leverandor = 'Assa Abloy'
                  AND c.Navn LIKE 'SK5968/%'
                  AND EXISTS (SELECT 1 FROM Produktgrupper WHERE Navn = 'Låser og låssylindere')
                  AND NOT EXISTS (
                      SELECT 1 FROM ComponentProduktgrupper cp
                      WHERE cp.KomponenterId = c.Id
                        AND cp.ProduktgrupperId = (SELECT Id FROM Produktgrupper WHERE Navn = 'Låser og låssylindere')
                  );
                """);

            var seedDir = Path.Combine(AppContext.BaseDirectory, "Data", "Seed");

            var pdfPath = Path.Combine(seedDir, "FDV-Samlehefte-AAOSN.pdf");
            if (File.Exists(pdfPath))
            {
                var hex = Convert.ToHexString(File.ReadAllBytes(pdfPath));
                migrationBuilder.Sql(
                    $"""
                    UPDATE Components
                    SET FdvData = X'{hex}',
                        FdvFilnavn = 'FDV_Samlehefte_AAOSN-REV-aug22.pdf',
                        FdvContentType = 'application/pdf'
                    WHERE FdvData IS NULL
                      AND Aktiv = 1
                      AND Leverandor = 'Assa Abloy'
                      AND Navn LIKE 'SK5968/%';
                    """);
            }

            void SettBilde(string filnavn, string navnSuffiks)
            {
                var bildePath = Path.Combine(seedDir, filnavn);
                if (!File.Exists(bildePath))
                {
                    return;
                }

                var hex = Convert.ToHexString(File.ReadAllBytes(bildePath));
                migrationBuilder.Sql(
                    $"""
                    UPDATE Components
                    SET BildeData = X'{hex}',
                        BildeFilnavn = '{filnavn}',
                        BildeContentType = 'image/png'
                    WHERE BildeData IS NULL
                      AND Aktiv = 1
                      AND Leverandor = 'Assa Abloy'
                      AND Navn LIKE 'SK5968/%'
                      AND Navn LIKE '%{navnSuffiks}';
                    """);
            }

            SettBilde("Sylinderskilt-InnFkr.png", "Inn Fkr");
            SettBilde("Sylinderskilt-UtvFkr.png", "Utv Fkr");
            SettBilde("Sylinderskilt-InnFkrm.png", "Inn Fkrm");
            SettBilde("Sylinderskilt-UtvFkrm.png", "Utv Fkrm");
            SettBilde("Sylinderskilt-InnMsm.png", "Inn Msm");
            SettBilde("Sylinderskilt-UtvMsm.png", "Utv Msm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
