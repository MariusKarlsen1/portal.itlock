using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PrisKodeTimetype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Timetype",
                table: "PrisKoder",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Eksisterende overtidskoder får riktig timetype (1 = 50 %, 2 = 100 %).
            migrationBuilder.Sql("UPDATE PrisKoder SET Timetype = 1 WHERE Type = 0 AND lower(Navn) LIKE '%overtid%50%';");
            migrationBuilder.Sql("UPDATE PrisKoder SET Timetype = 2 WHERE Type = 0 AND lower(Navn) LIKE '%overtid%100%';");

            // Har organisasjonen 110 (overtid 50 %) men ingen 111, legges 111
            // (overtid 100 %) til med priser regnet ut fra 110.
            migrationBuilder.Sql(@"INSERT INTO PrisKoder (Kode, Navn, Beskrivelse, Type, Enhet, Kostpris, Salgspris, ErMontasje, Aktiv, Rekkefolge, Timetype)
                SELECT '111', 'Montasje, overtid 100 %', NULL, Type, Enhet,
                       printf('%d', round(CAST(Kostpris AS REAL) / 1.35 * 1.7)), printf('%d', round(CAST(Salgspris AS REAL) / 1.5 * 2)),
                       0, 1, Rekkefolge, 2
                FROM PrisKoder
                WHERE Kode = '110' AND NOT EXISTS (SELECT 1 FROM PrisKoder WHERE Kode = '111');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timetype",
                table: "PrisKoder");
        }
    }
}
