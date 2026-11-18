using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KundeFastTimepris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FastTimeprisMontasje",
                table: "Kunder",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FastTimeprisProsjektledelse",
                table: "Kunder",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FastTimeprisMontasje",
                table: "Kunder");

            migrationBuilder.DropColumn(
                name: "FastTimeprisProsjektledelse",
                table: "Kunder");
        }
    }
}
