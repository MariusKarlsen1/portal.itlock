using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KodeLinjeMontasje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErMontasje",
                table: "TilbudKodeLinjer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Montasjelinjene merkes: linjer fra montasjekoden, og linjen som følger varenes montasjetid.
            migrationBuilder.Sql(@"
                UPDATE TilbudKodeLinjer SET ErMontasje = 1
                WHERE Type = 0 AND Enhet = 0
                  AND (FraMontasjetid = 1 OR PrisKodeId IN (SELECT Id FROM PrisKoder WHERE ErMontasje = 1));");

            // Priskoden for rigg og drift regnes av montasjetimene (prosent × timer × timepris).
            migrationBuilder.Sql(@"
                UPDATE PrisKoder SET Enhet = 6
                WHERE Enhet = 5 AND lower(Navn) LIKE '%rigg%';");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErMontasje",
                table: "TilbudKodeLinjer");

        }
    }
}
