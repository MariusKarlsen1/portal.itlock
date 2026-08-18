using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudLinjePrisTypeOgRabatt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrisType",
                table: "TilbudLinjer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Prosentsats",
                table: "TilbudLinjer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RabattProsent",
                table: "TilbudLinjer",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrisType",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "Prosentsats",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "RabattProsent",
                table: "TilbudLinjer");
        }
    }
}
