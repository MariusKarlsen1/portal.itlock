using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class AvvikUtvidet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnsvarligBrukerId",
                table: "Avvik",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArbeidsordreId",
                table: "Avvik",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LukketDato",
                table: "Avvik",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Nummer",
                table: "Avvik",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Prioritet",
                table: "Avvik",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SynligForKunde",
                table: "Avvik",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tittel",
                table: "Avvik",
                type: "TEXT",
                nullable: true);

            // Eksisterende avvik: normal prioritet og løpenummer i opprettet-rekkefølge.
            migrationBuilder.Sql("UPDATE Avvik SET Prioritet = 1;");
            migrationBuilder.Sql("UPDATE Avvik SET Nummer = (SELECT COUNT(*) FROM Avvik a2 WHERE a2.Id <= Avvik.Id);");

            migrationBuilder.CreateTable(
                name: "AvvikKommentarer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvvikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    ErHendelse = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvvikKommentarer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvvikKommentarer_Avvik_AvvikId",
                        column: x => x.AvvikId,
                        principalTable: "Avvik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvvikKommentarer_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AvvikMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvvikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    Rotasjon = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvvikMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvvikMedia_Avvik_AvvikId",
                        column: x => x.AvvikId,
                        principalTable: "Avvik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avvik_AnsvarligBrukerId",
                table: "Avvik",
                column: "AnsvarligBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_Avvik_ArbeidsordreId",
                table: "Avvik",
                column: "ArbeidsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_AvvikKommentarer_AvvikId",
                table: "AvvikKommentarer",
                column: "AvvikId");

            migrationBuilder.CreateIndex(
                name: "IX_AvvikKommentarer_BrukerId",
                table: "AvvikKommentarer",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_AvvikMedia_AvvikId",
                table: "AvvikMedia",
                column: "AvvikId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avvik_Arbeidsordre_ArbeidsordreId",
                table: "Avvik",
                column: "ArbeidsordreId",
                principalTable: "Arbeidsordre",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Avvik_Brukere_AnsvarligBrukerId",
                table: "Avvik",
                column: "AnsvarligBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avvik_Arbeidsordre_ArbeidsordreId",
                table: "Avvik");

            migrationBuilder.DropForeignKey(
                name: "FK_Avvik_Brukere_AnsvarligBrukerId",
                table: "Avvik");

            migrationBuilder.DropTable(
                name: "AvvikKommentarer");

            migrationBuilder.DropTable(
                name: "AvvikMedia");

            migrationBuilder.DropIndex(
                name: "IX_Avvik_AnsvarligBrukerId",
                table: "Avvik");

            migrationBuilder.DropIndex(
                name: "IX_Avvik_ArbeidsordreId",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "AnsvarligBrukerId",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "ArbeidsordreId",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "LukketDato",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "Nummer",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "Prioritet",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "SynligForKunde",
                table: "Avvik");

            migrationBuilder.DropColumn(
                name: "Tittel",
                table: "Avvik");
        }
    }
}
