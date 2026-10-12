using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class RessursplanKategori : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF scaffolder tom streng som standard. Modellen har "internt",
            // og de hendelsene som alt ligger der er nettopp internt arbeid -
            // uten dette ville de falt utenfor alle kategoriene i filteret.
            migrationBuilder.AddColumn<string>(
                name: "Kategori",
                table: "Ressursplaner",
                type: "TEXT",
                nullable: false,
                defaultValue: "internt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "Ressursplaner");
        }
    }
}
