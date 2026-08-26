using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsBibliotekOgProsjektLenke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BildeContentType",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "BildeData",
                table: "KoblingsSymboler");

            migrationBuilder.AddColumn<int>(
                name: "SymbolBibliotekId",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProsjektId",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KoblingsSymbolBibliotek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kategori = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: true),
                    BildeData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    BildeContentType = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsSymbolBibliotek", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsSymboler_SymbolBibliotekId",
                table: "KoblingsSymboler",
                column: "SymbolBibliotekId");

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsSkjemaer_ProsjektId",
                table: "KoblingsSkjemaer",
                column: "ProsjektId");

            migrationBuilder.AddForeignKey(
                name: "FK_KoblingsSkjemaer_Prosjekter_ProsjektId",
                table: "KoblingsSkjemaer",
                column: "ProsjektId",
                principalTable: "Prosjekter",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_KoblingsSymboler_KoblingsSymbolBibliotek_SymbolBibliotekId",
                table: "KoblingsSymboler",
                column: "SymbolBibliotekId",
                principalTable: "KoblingsSymbolBibliotek",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KoblingsSkjemaer_Prosjekter_ProsjektId",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropForeignKey(
                name: "FK_KoblingsSymboler_KoblingsSymbolBibliotek_SymbolBibliotekId",
                table: "KoblingsSymboler");

            migrationBuilder.DropTable(
                name: "KoblingsSymbolBibliotek");

            migrationBuilder.DropIndex(
                name: "IX_KoblingsSymboler_SymbolBibliotekId",
                table: "KoblingsSymboler");

            migrationBuilder.DropIndex(
                name: "IX_KoblingsSkjemaer_ProsjektId",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropColumn(
                name: "SymbolBibliotekId",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "ProsjektId",
                table: "KoblingsSkjemaer");

            migrationBuilder.AddColumn<string>(
                name: "BildeContentType",
                table: "KoblingsSymboler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BildeData",
                table: "KoblingsSymboler",
                type: "BLOB",
                nullable: true);
        }
    }
}
