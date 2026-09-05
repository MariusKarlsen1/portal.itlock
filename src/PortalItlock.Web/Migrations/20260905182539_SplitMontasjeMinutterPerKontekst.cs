using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SplitMontasjeMinutterPerKontekst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MontasjeMinutter",
                table: "Components",
                newName: "MontasjeMinutterProsjekt");

            migrationBuilder.AddColumn<int>(
                name: "MontasjeMinutterArbeidsordre",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MontasjeMinutterService",
                table: "Components",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontasjeMinutterArbeidsordre",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "MontasjeMinutterService",
                table: "Components");

            migrationBuilder.RenameColumn(
                name: "MontasjeMinutterProsjekt",
                table: "Components",
                newName: "MontasjeMinutter");
        }
    }
}
