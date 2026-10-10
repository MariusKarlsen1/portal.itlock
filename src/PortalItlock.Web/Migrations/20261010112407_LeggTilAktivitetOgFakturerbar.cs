using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilAktivitetOgFakturerbar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Aktivitet",
                table: "Timeregistreringer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Standardverdien må være true, som på modellen - ellers ville
            // alle EKSISTERENDE timeregistreringer blitt merket som ikke
            // fakturerbare i det øyeblikket kolonnen legges til.
            migrationBuilder.AddColumn<bool>(
                name: "Fakturerbar",
                table: "Timeregistreringer",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aktivitet",
                table: "Timeregistreringer");

            migrationBuilder.DropColumn(
                name: "Fakturerbar",
                table: "Timeregistreringer");
        }
    }
}
