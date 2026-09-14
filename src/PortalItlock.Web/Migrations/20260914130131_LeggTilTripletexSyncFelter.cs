using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTripletexSyncFelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TripletexOrdreFeil",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripletexOrdreId",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripletexOrdreNummer",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TripletexOrdreSendtDato",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripletexSyncTilstand",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SistSynkronisertKunderUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripletexSyncTilstand", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripletexSyncTilstand");

            migrationBuilder.DropColumn(
                name: "TripletexOrdreFeil",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "TripletexOrdreId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "TripletexOrdreNummer",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "TripletexOrdreSendtDato",
                table: "Arbeidsordre");
        }
    }
}
