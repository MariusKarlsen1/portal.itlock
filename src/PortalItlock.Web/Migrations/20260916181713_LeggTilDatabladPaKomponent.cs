using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDatabladPaKomponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatabladContentType",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "DatabladData",
                table: "Components",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabladFilnavn",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComponentDatabladDokumenter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentDatabladDokumenter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentDatabladDokumenter_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentDatabladDokumenter_ComponentId",
                table: "ComponentDatabladDokumenter",
                column: "ComponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentDatabladDokumenter");

            migrationBuilder.DropColumn(
                name: "DatabladContentType",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "DatabladData",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "DatabladFilnavn",
                table: "Components");
        }
    }
}
