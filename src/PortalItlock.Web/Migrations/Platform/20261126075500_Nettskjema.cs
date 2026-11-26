using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class Nettskjema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SkjemaBilder",
                table: "TenantInnstillinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SkjemaPa",
                table: "TenantInnstillinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SkjemaValg",
                table: "TenantInnstillinger",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkjemaBilder",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "SkjemaPa",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "SkjemaValg",
                table: "TenantInnstillinger");
        }
    }
}
