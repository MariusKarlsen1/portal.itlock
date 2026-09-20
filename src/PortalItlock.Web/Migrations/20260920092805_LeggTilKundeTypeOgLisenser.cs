using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKundeTypeOgLisenser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Kunder",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AntallBatterier",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Batteritype",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KreverBatterier",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "KundeLisenser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    UtlopsDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notat = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KundeLisenser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KundeLisenser_Kunder_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LisensVarselSendt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SistSendtDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LisensVarselSendt", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KundeLisenser_KundeId",
                table: "KundeLisenser",
                column: "KundeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KundeLisenser");

            migrationBuilder.DropTable(
                name: "LisensVarselSendt");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Kunder");

            migrationBuilder.DropColumn(
                name: "AntallBatterier",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Batteritype",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "KreverBatterier",
                table: "Components");
        }
    }
}
