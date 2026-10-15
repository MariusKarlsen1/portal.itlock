using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServiceavtaleSjekkpunkter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceavtaleSjekkpunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceavtaleSjekkpunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceavtaleSjekkpunkter_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceavtaleSjekkpunkter_ProsjektId",
                table: "ServiceavtaleSjekkpunkter",
                column: "ProsjektId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceavtaleSjekkpunkter");
        }
    }
}
