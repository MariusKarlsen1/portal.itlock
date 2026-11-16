using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DorOpsjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DorId",
                table: "TilbudLinjer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpsjonDorer",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OpsjonPerDor",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ErOpsjon",
                table: "DorKomponenter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DorId",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "OpsjonDorer",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "OpsjonPerDor",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "ErOpsjon",
                table: "DorKomponenter");
        }
    }
}
