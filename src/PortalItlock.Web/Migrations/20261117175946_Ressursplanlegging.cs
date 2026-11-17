using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Ressursplanlegging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UkeplanKapasitet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ar = table.Column<int>(type: "INTEGER", nullable: false),
                    Uke = table.Column<int>(type: "INTEGER", nullable: false),
                    Timer = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UkeplanKapasitet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UkeplanRader",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: true),
                    Kategori = table.Column<int>(type: "INTEGER", nullable: false),
                    AnsvarligId = table.Column<int>(type: "INTEGER", nullable: true),
                    Sannsynlighet = table.Column<int>(type: "INTEGER", nullable: false),
                    Skjult = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UkeplanRader", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UkeplanRader_Brukere_AnsvarligId",
                        column: x => x.AnsvarligId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UkeplanRader_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UkeplanEndringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    RadId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Under = table.Column<string>(type: "TEXT", nullable: true),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kvittert = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UkeplanEndringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UkeplanEndringer_UkeplanRader_RadId",
                        column: x => x.RadId,
                        principalTable: "UkeplanRader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UkeplanTimer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ar = table.Column<int>(type: "INTEGER", nullable: false),
                    Uke = table.Column<int>(type: "INTEGER", nullable: false),
                    Timer = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UkeplanTimer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UkeplanTimer_UkeplanRader_RadId",
                        column: x => x.RadId,
                        principalTable: "UkeplanRader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UkeplanEndringer_RadId",
                table: "UkeplanEndringer",
                column: "RadId");

            migrationBuilder.CreateIndex(
                name: "IX_UkeplanKapasitet_Ar_Uke",
                table: "UkeplanKapasitet",
                columns: new[] { "Ar", "Uke" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UkeplanRader_AnsvarligId",
                table: "UkeplanRader",
                column: "AnsvarligId");

            migrationBuilder.CreateIndex(
                name: "IX_UkeplanRader_ProsjektId",
                table: "UkeplanRader",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_UkeplanTimer_RadId_Ar_Uke",
                table: "UkeplanTimer",
                columns: new[] { "RadId", "Ar", "Uke" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UkeplanEndringer");

            migrationBuilder.DropTable(
                name: "UkeplanKapasitet");

            migrationBuilder.DropTable(
                name: "UkeplanTimer");

            migrationBuilder.DropTable(
                name: "UkeplanRader");
        }
    }
}
