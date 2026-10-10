using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKoblingsEditorInnstillinger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KomponentNokkel",
                table: "KoblingsSymboler",
                type: "TEXT",
                nullable: true);

            // Standardverdiene her må speile standardverdiene på modellen
            // (true/true/true/"Standard"), ellers ville alle EKSISTERENDE
            // skjema fått rutenett, snapping og komponentnavn slått AV og en
            // tom stil når kolonnene legges til.
            migrationBuilder.AddColumn<bool>(
                name: "SnapTilRutenett",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Stil",
                table: "KoblingsSkjemaer",
                type: "TEXT",
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<bool>(
                name: "VisKomponentnavn",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "VisRutenett",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KomponentNokkel",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "SnapTilRutenett",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropColumn(
                name: "Stil",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropColumn(
                name: "VisKomponentnavn",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropColumn(
                name: "VisRutenett",
                table: "KoblingsSkjemaer");
        }
    }
}
