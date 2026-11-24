using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreDirekte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Listepris",
                table: "ArbeidsordreVarer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RabattProsent",
                table: "ArbeidsordreVarer",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ArbeidOverstyringJson",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErDirekte",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Listepris",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "RabattProsent",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "ArbeidOverstyringJson",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "ErDirekte",
                table: "Arbeidsordre");
        }
    }
}
