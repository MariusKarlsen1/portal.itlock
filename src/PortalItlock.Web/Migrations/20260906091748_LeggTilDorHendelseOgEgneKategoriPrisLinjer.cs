using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDorHendelseOgEgneKategoriPrisLinjer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErEgendefinert",
                table: "ServiceKategoriPriser",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ManueltAntall",
                table: "ServiceKategoriPriser",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DorHendelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    UtfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorHendelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DorHendelser_Brukere_UtfortAvBrukerId",
                        column: x => x.UtfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DorHendelser_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DorHendelser_DorId",
                table: "DorHendelser",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_DorHendelser_UtfortAvBrukerId",
                table: "DorHendelser",
                column: "UtfortAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DorHendelser");

            migrationBuilder.DropColumn(
                name: "ErEgendefinert",
                table: "ServiceKategoriPriser");

            migrationBuilder.DropColumn(
                name: "ManueltAntall",
                table: "ServiceKategoriPriser");
        }
    }
}
