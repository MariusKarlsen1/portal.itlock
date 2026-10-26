using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsKabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KabelType",
                table: "KoblingsStreker",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LedereJson",
                table: "KoblingsStreker",
                type: "TEXT",
                nullable: true);

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
                name: "KabelType",
                table: "KoblingsStreker");

            migrationBuilder.DropColumn(
                name: "LedereJson",
                table: "KoblingsStreker");

            migrationBuilder.DropColumn(
                name: "Navn",
                table: "KoblingsStreker");
        }
    }
}
