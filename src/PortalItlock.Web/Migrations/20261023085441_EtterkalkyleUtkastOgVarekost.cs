using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class EtterkalkyleUtkastOgVarekost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErUtkast",
                table: "ProsjektEtterkalkyler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "FaktiskVarekost",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErUtkast",
                table: "ProsjektEtterkalkyler");

            migrationBuilder.DropColumn(
                name: "FaktiskVarekost",
                table: "Prosjekter");
        }
    }
}
