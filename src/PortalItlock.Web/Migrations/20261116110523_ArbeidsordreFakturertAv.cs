using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreFakturertAv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FakturertAvBrukerId",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FakturertAvNavn",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FakturertAvBrukerId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "FakturertAvNavn",
                table: "Arbeidsordre");
        }
    }
}
