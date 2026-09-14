using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTripletexKontoPaKomponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripletexKontoId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripletexKontoNavn",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripletexKontoNummer",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripletexProduktId",
                table: "Components",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TripletexKontoId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "TripletexKontoNavn",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "TripletexKontoNummer",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "TripletexProduktId",
                table: "Components");
        }
    }
}
