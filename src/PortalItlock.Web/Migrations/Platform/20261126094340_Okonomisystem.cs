using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class Okonomisystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Okonomisystem",
                table: "TenantInnstillinger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OkonomisystemNavn",
                table: "TenantInnstillinger",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Okonomisystem",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "OkonomisystemNavn",
                table: "TenantInnstillinger");
        }
    }
}
