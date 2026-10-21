using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class Rabattbrytere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ArligRabattAktiv",
                table: "PlattformInnstillinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "VolumrabattAktiv",
                table: "PlattformInnstillinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArligRabattAktiv",
                table: "PlattformInnstillinger");

            migrationBuilder.DropColumn(
                name: "VolumrabattAktiv",
                table: "PlattformInnstillinger");
        }
    }
}
