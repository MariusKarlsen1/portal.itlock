using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KomponentErEgendefinert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErEgendefinert",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Varer som allerede er lagt inn som egendefinert på en dør.
            migrationBuilder.Sql("UPDATE \"Components\" SET \"ErEgendefinert\" = 1 WHERE \"Varegruppe\" = 'Egendefinert';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErEgendefinert",
                table: "Components");
        }
    }
}
