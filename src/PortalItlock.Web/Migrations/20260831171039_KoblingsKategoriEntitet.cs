using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsKategoriEntitet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Kategori",
                table: "KoblingsSkjemaer",
                newName: "KategoriId");

            // Den gamle enum-kolonnen var 0-indeksert (ARX=0, Salto=1, Diverse=2), mens de nye
            // KoblingsKategorier-radene under er 1-indeksert (Id 1/2/3) - skift verdiene tilsvarende
            // slik at eksisterende koblingsskjema fortsatt peker på riktig kategori.
            migrationBuilder.Sql("UPDATE \"KoblingsSkjemaer\" SET \"KategoriId\" = \"KategoriId\" + 1;");

            migrationBuilder.CreateTable(
                name: "KoblingsKategorier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsKategorier", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "KoblingsKategorier",
                columns: new[] { "Id", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 1, "ARX", 1 },
                    { 2, "Salto", 2 },
                    { 3, "Diverse", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsSkjemaer_KategoriId",
                table: "KoblingsSkjemaer",
                column: "KategoriId");

            migrationBuilder.AddForeignKey(
                name: "FK_KoblingsSkjemaer_KoblingsKategorier_KategoriId",
                table: "KoblingsSkjemaer",
                column: "KategoriId",
                principalTable: "KoblingsKategorier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KoblingsSkjemaer_KoblingsKategorier_KategoriId",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropTable(
                name: "KoblingsKategorier");

            migrationBuilder.DropIndex(
                name: "IX_KoblingsSkjemaer_KategoriId",
                table: "KoblingsSkjemaer");

            migrationBuilder.Sql("UPDATE \"KoblingsSkjemaer\" SET \"KategoriId\" = \"KategoriId\" - 1;");

            migrationBuilder.RenameColumn(
                name: "KategoriId",
                table: "KoblingsSkjemaer",
                newName: "Kategori");
        }
    }
}
