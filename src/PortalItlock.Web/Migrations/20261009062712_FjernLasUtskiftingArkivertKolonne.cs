using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class FjernLasUtskiftingArkivertKolonne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Arkivert",
                table: "LasUtskiftinger");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Arkivert",
                table: "LasUtskiftinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
