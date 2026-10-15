using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KundeOppfolginger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KundeOppfolginger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Frist = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnsvarligBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    Notat = table.Column<string>(type: "TEXT", nullable: true),
                    VarselPortal = table.Column<bool>(type: "INTEGER", nullable: false),
                    VarselEpost = table.Column<bool>(type: "INTEGER", nullable: false),
                    VarselSms = table.Column<bool>(type: "INTEGER", nullable: false),
                    VarselDagerFor = table.Column<int>(type: "INTEGER", nullable: false),
                    Gjenta = table.Column<int>(type: "INTEGER", nullable: false),
                    EpostVarselSendt = table.Column<bool>(type: "INTEGER", nullable: false),
                    Utfort = table.Column<bool>(type: "INTEGER", nullable: false),
                    UtfortDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UtfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    UtfortKommentar = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KundeOppfolginger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KundeOppfolginger_Brukere_AnsvarligBrukerId",
                        column: x => x.AnsvarligBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KundeOppfolginger_Brukere_UtfortAvBrukerId",
                        column: x => x.UtfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KundeOppfolginger_Kunder_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KundeOppfolginger_AnsvarligBrukerId",
                table: "KundeOppfolginger",
                column: "AnsvarligBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_KundeOppfolginger_KundeId_Utfort_Frist",
                table: "KundeOppfolginger",
                columns: new[] { "KundeId", "Utfort", "Frist" });

            migrationBuilder.CreateIndex(
                name: "IX_KundeOppfolginger_UtfortAvBrukerId",
                table: "KundeOppfolginger",
                column: "UtfortAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KundeOppfolginger");
        }
    }
}
