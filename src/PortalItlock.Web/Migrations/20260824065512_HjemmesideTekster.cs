using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class HjemmesideTekster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HjemmesideTekster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Ingress = table.Column<string>(type: "TEXT", nullable: false),
                    MontorerTittel = table.Column<string>(type: "TEXT", nullable: false),
                    MontorerBeskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    ProsjektlederTittel = table.Column<string>(type: "TEXT", nullable: false),
                    ProsjektlederBeskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    AdminTittel = table.Column<string>(type: "TEXT", nullable: false),
                    AdminBeskrivelse = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HjemmesideTekster", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HjemmesideTekster",
                columns: new[] { "Id", "AdminBeskrivelse", "AdminTittel", "Ingress", "MontorerBeskrivelse", "MontorerTittel", "ProsjektlederBeskrivelse", "ProsjektlederTittel", "Tittel" },
                values: new object[] { 1, "Opprett og vedlikehold grunnlagsdata", "For admin", "Internt verktøy for prosjektering, befaring og det daglige arbeidet med dørpakker, komponenter og krav. Velg hvem du er, så finner du raskt det du trenger.", "Guide, befaring og vedlegg til bruk i felt", "For montører", "Systemer, prosjektering og prosjekter", "For prosjektledere", "Itlock Full Kontroll" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HjemmesideTekster");
        }
    }
}
