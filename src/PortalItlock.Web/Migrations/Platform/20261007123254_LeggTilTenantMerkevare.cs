using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class LeggTilTenantMerkevare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "Tenants",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalVisningsnavn",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PortalVisningsnavn",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TemaBakgrunn",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TemaFarge",
                table: "Tenants");
        }
    }
}
