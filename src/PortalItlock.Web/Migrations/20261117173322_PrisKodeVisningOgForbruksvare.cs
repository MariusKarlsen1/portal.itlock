using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PrisKodeVisningOgForbruksvare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MontorKanFore",
                table: "PrisKoder",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "VisPaTilbud",
                table: "PrisKoder",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErForbruksvare",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontorKanFore",
                table: "PrisKoder");

            migrationBuilder.DropColumn(
                name: "VisPaTilbud",
                table: "PrisKoder");

            migrationBuilder.DropColumn(
                name: "ErForbruksvare",
                table: "Components");
        }
    }
}
