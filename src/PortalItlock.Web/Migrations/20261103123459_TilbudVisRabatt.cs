using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudVisRabatt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisRabattIPdf",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Tilbud som allerede har rabatt viste den i PDF-en - de beholder det.
            migrationBuilder.Sql(@"
                UPDATE Tilbud SET VisRabattIPdf = 1
                WHERE PrisType = 3
                   OR Id IN (SELECT TilbudId FROM TilbudLinjer WHERE RabattProsent > 0 OR PrisType = 3);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisRabattIPdf",
                table: "Tilbud");

        }
    }
}
