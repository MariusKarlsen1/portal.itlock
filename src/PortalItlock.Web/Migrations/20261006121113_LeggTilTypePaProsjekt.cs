using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTypePaProsjekt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue 4 = ProsjektType.Annet - eksisterende prosjekter har
            // ingen reell kategorisering fra før, så de settes til "Annet" i
            // stedet for å feilaktig lande på 0 (Nybygg).
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Prosjekter");
        }
    }
}
