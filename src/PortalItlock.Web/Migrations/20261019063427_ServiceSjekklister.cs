using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServiceSjekklister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceSjekklisteId",
                table: "ServicerundeSjekklistepunkter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceSjekklisteId",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceSjekklister",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    ErStandard = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSjekklister", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeSjekklistepunkter_ServiceSjekklisteId",
                table: "ServicerundeSjekklistepunkter",
                column: "ServiceSjekklisteId");

            // Dagens felles sjekkliste blir liste nr. 1, «Standard», som brukes
            // på alle serviceavtaler som ikke har valgt en annen liste.
            migrationBuilder.Sql("INSERT INTO ServiceSjekklister (Navn, ErStandard) VALUES ('Standard', 1);");
            migrationBuilder.Sql("UPDATE ServicerundeSjekklistepunkter SET ServiceSjekklisteId = (SELECT Id FROM ServiceSjekklister WHERE ErStandard = 1 LIMIT 1) WHERE ServiceSjekklisteId IS NULL;");
            migrationBuilder.AddForeignKey(
                name: "FK_ServicerundeSjekklistepunkter_ServiceSjekklister_ServiceSjekklisteId",
                table: "ServicerundeSjekklistepunkter",
                column: "ServiceSjekklisteId",
                principalTable: "ServiceSjekklister",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServicerundeSjekklistepunkter_ServiceSjekklister_ServiceSjekklisteId",
                table: "ServicerundeSjekklistepunkter");

            migrationBuilder.DropTable(
                name: "ServiceSjekklister");

            migrationBuilder.DropIndex(
                name: "IX_ServicerundeSjekklistepunkter_ServiceSjekklisteId",
                table: "ServicerundeSjekklistepunkter");

            migrationBuilder.DropColumn(
                name: "ServiceSjekklisteId",
                table: "ServicerundeSjekklistepunkter");

            migrationBuilder.DropColumn(
                name: "ServiceSjekklisteId",
                table: "Prosjekter");
        }
    }
}
