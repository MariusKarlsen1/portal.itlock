using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ForesporselEpostHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InnholdHtml",
                table: "Foresporsler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentId",
                table: "ForesporselMedia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErInline",
                table: "ForesporselMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnholdHtml",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "ContentId",
                table: "ForesporselMedia");

            migrationBuilder.DropColumn(
                name: "ErInline",
                table: "ForesporselMedia");
        }
    }
}
