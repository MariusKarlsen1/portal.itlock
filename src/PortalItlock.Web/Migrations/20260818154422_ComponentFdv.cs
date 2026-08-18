using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ComponentFdv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FdvContentType",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "FdvData",
                table: "Components",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FdvFilnavn",
                table: "Components",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FdvContentType",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "FdvData",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "FdvFilnavn",
                table: "Components");
        }
    }
}
