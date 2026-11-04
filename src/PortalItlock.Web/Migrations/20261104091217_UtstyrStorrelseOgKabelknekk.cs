using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class UtstyrStorrelseOgKabelknekk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Bredde",
                table: "PlanUtstyr",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "Hoyde",
                table: "PlanUtstyr",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<string>(
                name: "Knekkpunkter",
                table: "PlanForbindelser",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bredde",
                table: "PlanUtstyr");

            migrationBuilder.DropColumn(
                name: "Hoyde",
                table: "PlanUtstyr");

            migrationBuilder.DropColumn(
                name: "Knekkpunkter",
                table: "PlanForbindelser");
        }
    }
}
