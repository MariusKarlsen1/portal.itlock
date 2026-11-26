using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudPdfMottaker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PdfMottaker",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PdfMottakerAdresse",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PdfMottakerKundeId",
                table: "Tilbud",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfMottakerNavn",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfMottaker",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "PdfMottakerAdresse",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "PdfMottakerKundeId",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "PdfMottakerNavn",
                table: "Tilbud");
        }
    }
}
