using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilInntektskontoOgFjernTripletexProduktkobling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TripletexKontoId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "TripletexKontoNavn",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "TripletexKontoNummer",
                table: "Components");

            migrationBuilder.RenameColumn(
                name: "TripletexProduktId",
                table: "Components",
                newName: "InntektskontoId");

            // Gamle TripletexProduktId-verdier (fra det tidligere produkt-synk-forsøket)
            // er ikke gyldige Inntektskonto-IDer i det nye, portal-native kontoregisteret -
            // nullstill dem slik at ingen vare peker på en tilfeldig konto-rad.
            migrationBuilder.Sql("UPDATE Components SET InntektskontoId = NULL;");

            migrationBuilder.CreateTable(
                name: "Inntektskontoer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inntektskontoer", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_InntektskontoId",
                table: "Components",
                column: "InntektskontoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Inntektskontoer_InntektskontoId",
                table: "Components",
                column: "InntektskontoId",
                principalTable: "Inntektskontoer",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_Inntektskontoer_InntektskontoId",
                table: "Components");

            migrationBuilder.DropTable(
                name: "Inntektskontoer");

            migrationBuilder.DropIndex(
                name: "IX_Components_InntektskontoId",
                table: "Components");

            migrationBuilder.RenameColumn(
                name: "InntektskontoId",
                table: "Components",
                newName: "TripletexProduktId");

            migrationBuilder.AddColumn<int>(
                name: "TripletexKontoId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripletexKontoNavn",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripletexKontoNummer",
                table: "Components",
                type: "INTEGER",
                nullable: true);
        }
    }
}
