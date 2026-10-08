using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilLokasjonOgVedlegg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Befaringer",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Befaringer",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BefaringVedlegg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BefaringId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BefaringVedlegg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BefaringVedlegg_Befaringer_BefaringId",
                        column: x => x.BefaringId,
                        principalTable: "Befaringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BefaringVedlegg_BefaringId",
                table: "BefaringVedlegg",
                column: "BefaringId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BefaringVedlegg");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Befaringer");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Befaringer");
        }
    }
}
