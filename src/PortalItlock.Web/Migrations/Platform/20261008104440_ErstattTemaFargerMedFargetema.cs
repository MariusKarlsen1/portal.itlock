using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class ErstattTemaFargerMedFargetema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemaBakgrunn",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TemaFarge",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "Tema",
                table: "Tenants",
                type: "TEXT",
                nullable: false,
                defaultValue: "Standard");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tema",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "TemaBakgrunn",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemaFarge",
                table: "Tenants",
                type: "TEXT",
                nullable: true);
        }
    }
}
