using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilNokkelsystemDashboardFelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BildeContentType",
                table: "Nokkelsystemer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BildeData",
                table: "Nokkelsystemer",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Nokkelsystemer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Nokkelsystemer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NokkelsystemHendelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NokkelsystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    UtfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NokkelsystemHendelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NokkelsystemHendelser_Brukere_UtfortAvBrukerId",
                        column: x => x.UtfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NokkelsystemHendelser_Nokkelsystemer_NokkelsystemId",
                        column: x => x.NokkelsystemId,
                        principalTable: "Nokkelsystemer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NokkelsystemHendelser_NokkelsystemId",
                table: "NokkelsystemHendelser",
                column: "NokkelsystemId");

            migrationBuilder.CreateIndex(
                name: "IX_NokkelsystemHendelser_UtfortAvBrukerId",
                table: "NokkelsystemHendelser",
                column: "UtfortAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NokkelsystemHendelser");

            migrationBuilder.DropColumn(
                name: "BildeContentType",
                table: "Nokkelsystemer");

            migrationBuilder.DropColumn(
                name: "BildeData",
                table: "Nokkelsystemer");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Nokkelsystemer");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Nokkelsystemer");
        }
    }
}
