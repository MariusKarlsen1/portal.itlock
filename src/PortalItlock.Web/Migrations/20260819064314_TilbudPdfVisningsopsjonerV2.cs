using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudPdfVisningsopsjonerV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VisProduktkode",
                table: "Tilbud",
                newName: "VisAlleDorerFraBeslagsliste");

            migrationBuilder.RenameColumn(
                name: "VisKunTotalsum",
                table: "Tilbud",
                newName: "SummerAlleBeslag");

            migrationBuilder.AddColumn<bool>(
                name: "SkjulVarenummerISammendrag",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // De ombenevnte kolonnene arver rå verdier fra de gamle feltene, som hadde
            // motsatt betydning. Rett opp eksisterende rader slik at oppførselen forblir uendret.
            migrationBuilder.Sql("UPDATE \"Tilbud\" SET \"SummerAlleBeslag\" = 1, \"VisAlleDorerFraBeslagsliste\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkjulVarenummerISammendrag",
                table: "Tilbud");

            migrationBuilder.RenameColumn(
                name: "VisAlleDorerFraBeslagsliste",
                table: "Tilbud",
                newName: "VisProduktkode");

            migrationBuilder.RenameColumn(
                name: "SummerAlleBeslag",
                table: "Tilbud",
                newName: "VisKunTotalsum");
        }
    }
}
