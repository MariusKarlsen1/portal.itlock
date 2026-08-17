using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreOgTimeregistrering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Montorer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Montorer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Arbeidsordre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    AnsvarligMontorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arbeidsordre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arbeidsordre_Montorer_AnsvarligMontorId",
                        column: x => x.AnsvarligMontorId,
                        principalTable: "Montorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Arbeidsordre_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Timeregistreringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Start = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Slutt = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    PauseMinutter = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Kommentar = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timeregistreringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timeregistreringer_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Timeregistreringer_Montorer_MontorId",
                        column: x => x.MontorId,
                        principalTable: "Montorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Arbeidsordre_AnsvarligMontorId",
                table: "Arbeidsordre",
                column: "AnsvarligMontorId");

            migrationBuilder.CreateIndex(
                name: "IX_Arbeidsordre_ProsjektId",
                table: "Arbeidsordre",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_Timeregistreringer_ArbeidsordreId",
                table: "Timeregistreringer",
                column: "ArbeidsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_Timeregistreringer_MontorId",
                table: "Timeregistreringer",
                column: "MontorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timeregistreringer");

            migrationBuilder.DropTable(
                name: "Arbeidsordre");

            migrationBuilder.DropTable(
                name: "Montorer");
        }
    }
}
