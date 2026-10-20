using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class TenantBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantBackuper",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    ErPlattform = table.Column<bool>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: true),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Storrelse = table.Column<long>(type: "INTEGER", nullable: false),
                    DatabaseStorrelse = table.Column<long>(type: "INTEGER", nullable: false),
                    Lagring = table.Column<int>(type: "INTEGER", nullable: false),
                    Sti = table.Column<string>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: true),
                    Kontrollert = table.Column<bool>(type: "INTEGER", nullable: false),
                    Feil = table.Column<string>(type: "TEXT", nullable: true),
                    Av = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBackuper", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantBackuper_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBackuper_TenantId_Tidspunkt",
                table: "TenantBackuper",
                columns: new[] { "TenantId", "Tidspunkt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantBackuper");
        }
    }
}
