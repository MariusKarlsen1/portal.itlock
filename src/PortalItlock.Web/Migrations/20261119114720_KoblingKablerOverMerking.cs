using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingKablerOverMerking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KablerOver",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TekstFarge",
                table: "KoblingsStreker",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TekstVedEnden",
                table: "KoblingsStreker",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KablerOver",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "TekstFarge",
                table: "KoblingsStreker");

            migrationBuilder.DropColumn(
                name: "TekstVedEnden",
                table: "KoblingsStreker");
        }
    }
}
