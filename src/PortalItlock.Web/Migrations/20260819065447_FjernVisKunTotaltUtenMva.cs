using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class FjernVisKunTotaltUtenMva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisKunTotaltUtenMva",
                table: "Tilbud");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisKunTotaltUtenMva",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
