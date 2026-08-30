using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LasplanReserveDorFelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DorTil",
                table: "LasplanReserver",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dornr",
                table: "LasplanReserver",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DorTil",
                table: "LasplanReserver");

            migrationBuilder.DropColumn(
                name: "Dornr",
                table: "LasplanReserver");
        }
    }
}
