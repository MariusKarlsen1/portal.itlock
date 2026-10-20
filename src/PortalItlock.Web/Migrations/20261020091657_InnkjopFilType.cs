using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class InnkjopFilType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "InnkjopsordreFiler",
                type: "TEXT",
                nullable: false,
                defaultValue: "ordrebekreftelse");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "InnkjopsordreFiler");
        }
    }
}
