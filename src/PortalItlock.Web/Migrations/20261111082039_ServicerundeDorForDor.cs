using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServicerundeDorForDor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DorForDor",
                table: "Servicerunder",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FullfortDato",
                table: "Servicerunder",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceKontrollpunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Servicetype = table.Column<string>(type: "TEXT", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceKontrollpunkter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicerundeDorer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicerundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dornummer = table.Column<string>(type: "TEXT", nullable: false),
                    Rom = table.Column<string>(type: "TEXT", nullable: true),
                    Etasje = table.Column<string>(type: "TEXT", nullable: true),
                    PlantegningId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlantegningNavn = table.Column<string>(type: "TEXT", nullable: true),
                    PosX = table.Column<double>(type: "REAL", nullable: true),
                    PosY = table.Column<double>(type: "REAL", nullable: true),
                    Kategori = table.Column<string>(type: "TEXT", nullable: true),
                    KomponenterJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notat = table.Column<string>(type: "TEXT", nullable: true),
                    KontrollertDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KontrollertAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicerundeDorer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicerundeDorer_Servicerunder_ServicerundeId",
                        column: x => x.ServicerundeId,
                        principalTable: "Servicerunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicerundeDorPunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicerundeDorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Kilde = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Kommentar = table.Column<string>(type: "TEXT", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicerundeDorPunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicerundeDorPunkter_ServicerundeDorer_ServicerundeDorId",
                        column: x => x.ServicerundeDorId,
                        principalTable: "ServicerundeDorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeDorer_DorId",
                table: "ServicerundeDorer",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeDorer_ServicerundeId",
                table: "ServicerundeDorer",
                column: "ServicerundeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeDorPunkter_ServicerundeDorId",
                table: "ServicerundeDorPunkter",
                column: "ServicerundeDorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceKontrollpunkter");

            migrationBuilder.DropTable(
                name: "ServicerundeDorPunkter");

            migrationBuilder.DropTable(
                name: "ServicerundeDorer");

            migrationBuilder.DropColumn(
                name: "DorForDor",
                table: "Servicerunder");

            migrationBuilder.DropColumn(
                name: "FullfortDato",
                table: "Servicerunder");
        }
    }
}
