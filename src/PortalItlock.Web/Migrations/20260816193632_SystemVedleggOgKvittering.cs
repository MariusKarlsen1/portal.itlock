using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SystemVedleggOgKvittering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NokkelKvitteringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NokkelsystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MottakerNavn = table.Column<string>(type: "TEXT", nullable: false),
                    NokkelBetegnelse = table.Column<string>(type: "TEXT", nullable: true),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    RekvirertAv = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NokkelKvitteringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NokkelKvitteringer_Nokkelsystemer_NokkelsystemId",
                        column: x => x.NokkelsystemId,
                        principalTable: "Nokkelsystemer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemVedlegg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NokkelsystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemVedlegg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemVedlegg_Nokkelsystemer_NokkelsystemId",
                        column: x => x.NokkelsystemId,
                        principalTable: "Nokkelsystemer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NokkelKvitteringer_NokkelsystemId",
                table: "NokkelKvitteringer",
                column: "NokkelsystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemVedlegg_NokkelsystemId",
                table: "SystemVedlegg",
                column: "NokkelsystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NokkelKvitteringer");

            migrationBuilder.DropTable(
                name: "SystemVedlegg");
        }
    }
}
