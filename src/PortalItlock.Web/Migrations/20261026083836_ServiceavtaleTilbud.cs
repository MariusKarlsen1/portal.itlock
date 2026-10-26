using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServiceavtaleTilbud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServicePrislinjer",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServicePrismodell",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ServiceavtaleTekst",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServicePrislinjer",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServicePrismodell",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceavtaleTekst",
                table: "Prosjekter");
        }
    }
}
