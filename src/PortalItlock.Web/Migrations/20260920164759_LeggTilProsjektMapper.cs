using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilProsjektMapper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProsjektMapper",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Sortering = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektMapper", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektMapper_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektMapper_ProsjektId",
                table: "ProsjektMapper",
                column: "ProsjektId");

            migrationBuilder.AddColumn<int>(
                name: "MappeId",
                table: "ProsjektVedlegg",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektVedlegg_MappeId",
                table: "ProsjektVedlegg",
                column: "MappeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProsjektVedlegg_ProsjektMapper_MappeId",
                table: "ProsjektVedlegg",
                column: "MappeId",
                principalTable: "ProsjektMapper",
                principalColumn: "Id");

            // Bevar eksisterende mappe-organisering fra den gamle frittekst-
            // "Type"-kolonnen: opprett en ekte ProsjektMappe pr. distinkt
            // (ProsjektId, Type)-par, og koble ProsjektVedlegg til den før
            // Type-kolonnen fjernes.
            migrationBuilder.Sql(
                """
                INSERT INTO "ProsjektMapper" ("ProsjektId", "Navn", "Sortering", "OpprettetDato")
                SELECT DISTINCT "ProsjektId", "Type", 0, CURRENT_TIMESTAMP
                FROM "ProsjektVedlegg"
                WHERE "Type" IS NOT NULL AND TRIM("Type") != '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "ProsjektVedlegg"
                SET "MappeId" = (
                    SELECT m."Id" FROM "ProsjektMapper" m
                    WHERE m."ProsjektId" = "ProsjektVedlegg"."ProsjektId" AND m."Navn" = "ProsjektVedlegg"."Type"
                )
                WHERE "Type" IS NOT NULL AND TRIM("Type") != '';
                """);

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ProsjektVedlegg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ProsjektVedlegg",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "ProsjektVedlegg"
                SET "Type" = (
                    SELECT m."Navn" FROM "ProsjektMapper" m
                    WHERE m."Id" = "ProsjektVedlegg"."MappeId"
                )
                WHERE "MappeId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ProsjektVedlegg_ProsjektMapper_MappeId",
                table: "ProsjektVedlegg");

            migrationBuilder.DropTable(
                name: "ProsjektMapper");

            migrationBuilder.DropIndex(
                name: "IX_ProsjektVedlegg_MappeId",
                table: "ProsjektVedlegg");

            migrationBuilder.DropColumn(
                name: "MappeId",
                table: "ProsjektVedlegg");
        }
    }
}
