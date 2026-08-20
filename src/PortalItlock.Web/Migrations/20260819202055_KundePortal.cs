using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KundePortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "Brukere",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Servicehenvendelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dortype = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    OnsketTidspunkt = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SvarFraItlock = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicehenvendelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicehenvendelser_Kunder_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicehenvendelseBilder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicehenvendelseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicehenvendelseBilder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicehenvendelseBilder_Servicehenvendelser_ServicehenvendelseId",
                        column: x => x.ServicehenvendelseId,
                        principalTable: "Servicehenvendelser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brukere_KundeId",
                table: "Brukere",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicehenvendelseBilder_ServicehenvendelseId",
                table: "ServicehenvendelseBilder",
                column: "ServicehenvendelseId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicehenvendelser_KundeId",
                table: "Servicehenvendelser",
                column: "KundeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Brukere_Kunder_KundeId",
                table: "Brukere",
                column: "KundeId",
                principalTable: "Kunder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brukere_Kunder_KundeId",
                table: "Brukere");

            migrationBuilder.DropTable(
                name: "ServicehenvendelseBilder");

            migrationBuilder.DropTable(
                name: "Servicehenvendelser");

            migrationBuilder.DropIndex(
                name: "IX_Brukere_KundeId",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "Brukere");
        }
    }
}
