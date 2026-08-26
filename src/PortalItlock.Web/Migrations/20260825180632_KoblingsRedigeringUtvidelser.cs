using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsRedigeringUtvidelser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "KoblingsSymbolBibliotek");

            migrationBuilder.AddColumn<string>(
                name: "Navn",
                table: "KoblingsStreker",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Navn",
                table: "KoblingsStreker");

            migrationBuilder.AddColumn<int>(
                name: "Kategori",
                table: "KoblingsSymbolBibliotek",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
