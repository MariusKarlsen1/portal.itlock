using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class NullstillMenyoppsett : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alle eksisterende brukere får standard sidemeny, som nye brukere:
            // ingen skjulte og ingen ekstra lenker. Moduler organisasjonen ikke
            // har, vises uansett ikke (PortalModulRegister.ModulerFor).
            migrationBuilder.Sql("UPDATE Brukere SET SkjulteNavLenker = NULL, EkstraNavLenker = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
