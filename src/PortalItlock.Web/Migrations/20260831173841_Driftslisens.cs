using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Driftslisens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Driftsmeldinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InnmeldtAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    LestAvAnsatt = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driftsmeldinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Driftsmeldinger_Brukere_InnmeldtAvBrukerId",
                        column: x => x.InnmeldtAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Driftsmeldinger_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriftsmeldingMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DriftsmeldingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftsmeldingMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriftsmeldingMedia_Driftsmeldinger_DriftsmeldingId",
                        column: x => x.DriftsmeldingId,
                        principalTable: "Driftsmeldinger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Driftsmeldinger_DorId",
                table: "Driftsmeldinger",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_Driftsmeldinger_InnmeldtAvBrukerId",
                table: "Driftsmeldinger",
                column: "InnmeldtAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_DriftsmeldingMedia_DriftsmeldingId",
                table: "DriftsmeldingMedia",
                column: "DriftsmeldingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriftsmeldingMedia");

            migrationBuilder.DropTable(
                name: "Driftsmeldinger");
        }
    }
}
