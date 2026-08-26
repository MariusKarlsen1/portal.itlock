using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsSkjema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KoblingsSkjemaer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kategori = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OppdatertDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsSkjemaer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KoblingsStreker",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KoblingsSkjemaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PunkterJson = table.Column<string>(type: "TEXT", nullable: false),
                    Farge = table.Column<string>(type: "TEXT", nullable: false),
                    Tykkelse = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsStreker", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KoblingsStreker_KoblingsSkjemaer_KoblingsSkjemaId",
                        column: x => x.KoblingsSkjemaId,
                        principalTable: "KoblingsSkjemaer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KoblingsSymboler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KoblingsSkjemaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: true),
                    BildeData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    BildeContentType = table.Column<string>(type: "TEXT", nullable: false),
                    PosX = table.Column<double>(type: "REAL", nullable: false),
                    PosY = table.Column<double>(type: "REAL", nullable: false),
                    Bredde = table.Column<double>(type: "REAL", nullable: false),
                    Hoyde = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsSymboler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KoblingsSymboler_KoblingsSkjemaer_KoblingsSkjemaId",
                        column: x => x.KoblingsSkjemaId,
                        principalTable: "KoblingsSkjemaer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsStreker_KoblingsSkjemaId",
                table: "KoblingsStreker",
                column: "KoblingsSkjemaId");

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsSymboler_KoblingsSkjemaId",
                table: "KoblingsSymboler",
                column: "KoblingsSkjemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KoblingsStreker");

            migrationBuilder.DropTable(
                name: "KoblingsSymboler");

            migrationBuilder.DropTable(
                name: "KoblingsSkjemaer");
        }
    }
}
