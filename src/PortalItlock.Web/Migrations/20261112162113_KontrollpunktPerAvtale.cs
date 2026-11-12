using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KontrollpunktPerAvtale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProsjektId",
                table: "ServiceKontrollpunkter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SkruddAv",
                table: "ServiceKontrollpunkter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProsjektId",
                table: "ServiceKontrollpunkter");

            migrationBuilder.DropColumn(
                name: "SkruddAv",
                table: "ServiceKontrollpunkter");
        }
    }
}
