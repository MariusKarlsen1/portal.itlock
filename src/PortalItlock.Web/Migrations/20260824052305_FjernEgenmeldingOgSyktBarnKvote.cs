using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class FjernEgenmeldingOgSyktBarnKvote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EgenmeldingKvote",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "SyktBarnKvote",
                table: "Brukere");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EgenmeldingKvote",
                table: "Brukere",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyktBarnKvote",
                table: "Brukere",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
