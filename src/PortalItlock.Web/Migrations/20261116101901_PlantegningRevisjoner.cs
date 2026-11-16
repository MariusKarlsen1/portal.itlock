using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PlantegningRevisjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RevidertDato",
                table: "Plantegninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revisjon",
                table: "Plantegninger",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlantegningRevisjoner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantegningId = table.Column<int>(type: "INTEGER", nullable: false),
                    Revisjon = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpplastetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErstattetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantegningRevisjoner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantegningRevisjoner_Plantegninger_PlantegningId",
                        column: x => x.PlantegningId,
                        principalTable: "Plantegninger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantegningRevisjoner_PlantegningId",
                table: "PlantegningRevisjoner",
                column: "PlantegningId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantegningRevisjoner");

            migrationBuilder.DropColumn(
                name: "RevidertDato",
                table: "Plantegninger");

            migrationBuilder.DropColumn(
                name: "Revisjon",
                table: "Plantegninger");
        }
    }
}
