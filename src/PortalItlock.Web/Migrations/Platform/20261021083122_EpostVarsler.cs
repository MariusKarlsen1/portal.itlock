using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class EpostVarsler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantBrukerVarsler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nokkel = table.Column<string>(type: "TEXT", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBrukerVarsler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantVarselRoller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nokkel = table.Column<string>(type: "TEXT", nullable: false),
                    Rolle = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVarselRoller", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrukerVarsler_TenantId_BrukerId_Nokkel",
                table: "TenantBrukerVarsler",
                columns: new[] { "TenantId", "BrukerId", "Nokkel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantVarselRoller_TenantId_Nokkel_Rolle",
                table: "TenantVarselRoller",
                columns: new[] { "TenantId", "Nokkel", "Rolle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantBrukerVarsler");

            migrationBuilder.DropTable(
                name: "TenantVarselRoller");
        }
    }
}
