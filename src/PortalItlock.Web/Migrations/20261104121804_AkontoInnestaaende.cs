using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class AkontoInnestaaende : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InnestaaendeProsent",
                table: "ProsjektAkontoer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SendtInnestaaende",
                table: "ProsjektAkontoer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnestaaendeProsent",
                table: "ProsjektAkontoer");

            migrationBuilder.DropColumn(
                name: "SendtInnestaaende",
                table: "ProsjektAkontoer");
        }
    }
}
