using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKundeBilde : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BildeContentType",
                table: "Kunder",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BildeData",
                table: "Kunder",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BildeFilnavn",
                table: "Kunder",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BildeContentType",
                table: "Kunder");

            migrationBuilder.DropColumn(
                name: "BildeData",
                table: "Kunder");

            migrationBuilder.DropColumn(
                name: "BildeFilnavn",
                table: "Kunder");
        }
    }
}
