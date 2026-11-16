using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DorPakke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PakkeFunksjonIder",
                table: "Dorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PakkeId",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PakkeKomponentIder",
                table: "Dorer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PakkeFunksjonIder",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "PakkeId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "PakkeKomponentIder",
                table: "Dorer");
        }
    }
}
