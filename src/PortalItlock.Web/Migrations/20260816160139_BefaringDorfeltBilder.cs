using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BefaringDorfeltBilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BefaringDorfeltBilder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BefaringDorfeltId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BefaringDorfeltBilder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BefaringDorfeltBilder_BefaringDorfelt_BefaringDorfeltId",
                        column: x => x.BefaringDorfeltId,
                        principalTable: "BefaringDorfelt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BefaringDorfeltBilder_BefaringDorfeltId",
                table: "BefaringDorfeltBilder",
                column: "BefaringDorfeltId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BefaringDorfeltBilder");
        }
    }
}
