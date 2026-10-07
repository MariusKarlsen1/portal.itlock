using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class LeggTilTenantOrganisasjonsfelterOgLisenser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Epost",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kundeansvarlig",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrgNr",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postnr",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SistOppdatert",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sted",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Tenants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantLisenser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    AntallTildelt = table.Column<int>(type: "INTEGER", nullable: false),
                    SistEndret = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLisenser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantLisenser_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLisenser_TenantId",
                table: "TenantLisenser",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantLisenser");

            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Epost",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Kundeansvarlig",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OrgNr",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Postnr",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SistOppdatert",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Sted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tenants");
        }
    }
}
