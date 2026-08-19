using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LevertAv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LevertAv",
                table: "TilbudLinjer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LevertAv",
                table: "PackageComponents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevertAv",
                table: "DorKomponenter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevertAv",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "LevertAv",
                table: "PackageComponents");

            migrationBuilder.DropColumn(
                name: "LevertAv",
                table: "DorKomponenter");
        }
    }
}
