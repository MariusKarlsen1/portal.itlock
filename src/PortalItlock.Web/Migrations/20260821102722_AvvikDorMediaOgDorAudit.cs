using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class AvvikDorMediaOgDorAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OppdatertAvBrukerId",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OppdatertDato",
                table: "Dorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpprettetDato",
                table: "Dorer",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Eksisterende dører har ingen reell opprettelsesdato registrert fra før;
            // sett den til nå fremfor å vise 01.01.0001 i UI.
            migrationBuilder.Sql("UPDATE Dorer SET OpprettetDato = datetime('now') WHERE OpprettetDato = '0001-01-01 00:00:00';");

            migrationBuilder.CreateTable(
                name: "Avvik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    UtbedringBeskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    SendtTilKundeDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Signatur = table.Column<byte[]>(type: "BLOB", nullable: true),
                    SignertAvNavn = table.Column<string>(type: "TEXT", nullable: true),
                    SignertDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avvik", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avvik_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Avvik_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DorMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    Tidspunkt = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DorMedia_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dorer_OppdatertAvBrukerId",
                table: "Dorer",
                column: "OppdatertAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_Avvik_DorId",
                table: "Avvik",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_Avvik_OpprettetAvBrukerId",
                table: "Avvik",
                column: "OpprettetAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_DorMedia_DorId",
                table: "DorMedia",
                column: "DorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dorer_Brukere_OppdatertAvBrukerId",
                table: "Dorer",
                column: "OppdatertAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dorer_Brukere_OppdatertAvBrukerId",
                table: "Dorer");

            migrationBuilder.DropTable(
                name: "Avvik");

            migrationBuilder.DropTable(
                name: "DorMedia");

            migrationBuilder.DropIndex(
                name: "IX_Dorer_OppdatertAvBrukerId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "OppdatertAvBrukerId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "OppdatertDato",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "OpprettetDato",
                table: "Dorer");
        }
    }
}
