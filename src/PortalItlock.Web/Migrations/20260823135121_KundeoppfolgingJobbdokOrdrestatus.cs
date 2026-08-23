using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KundeoppfolgingJobbdokOrdrestatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AntattLeveringsdato",
                table: "Servicehenvendelser",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnsketDato",
                table: "Servicehenvendelser",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NesteOppfolgingsDato",
                table: "Kunder",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastetOppAvKunde",
                table: "ArbeidsordreMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DokumentasjonEpostSendtDato",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KundeInfoForJobb",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KundeOppfolgingNotater",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KundeOppfolgingNotater", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KundeOppfolgingNotater_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KundeOppfolgingNotater_Kunder_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KundeOppfolgingNotater_KundeId",
                table: "KundeOppfolgingNotater",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_KundeOppfolgingNotater_OpprettetAvBrukerId",
                table: "KundeOppfolgingNotater",
                column: "OpprettetAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KundeOppfolgingNotater");

            migrationBuilder.DropColumn(
                name: "AntattLeveringsdato",
                table: "Servicehenvendelser");

            migrationBuilder.DropColumn(
                name: "OnsketDato",
                table: "Servicehenvendelser");

            migrationBuilder.DropColumn(
                name: "NesteOppfolgingsDato",
                table: "Kunder");

            migrationBuilder.DropColumn(
                name: "LastetOppAvKunde",
                table: "ArbeidsordreMedia");

            migrationBuilder.DropColumn(
                name: "DokumentasjonEpostSendtDato",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "KundeInfoForJobb",
                table: "Arbeidsordre");
        }
    }
}
