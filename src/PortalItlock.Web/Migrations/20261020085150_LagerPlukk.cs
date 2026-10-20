using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LagerPlukk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Minimum",
                table: "VareLagerplasser",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Plukket",
                table: "LagerReservasjoner",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlukketLagerId",
                table: "LagerReservasjoner",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Minimum",
                table: "VareLagerplasser");

            migrationBuilder.DropColumn(
                name: "Plukket",
                table: "LagerReservasjoner");

            migrationBuilder.DropColumn(
                name: "PlukketLagerId",
                table: "LagerReservasjoner");
        }
    }
}
