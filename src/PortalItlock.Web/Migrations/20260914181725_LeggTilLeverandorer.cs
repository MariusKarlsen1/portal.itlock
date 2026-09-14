using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilLeverandorer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leverandorer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Postnr = table.Column<string>(type: "TEXT", nullable: true),
                    Sted = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leverandorer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentLeverandorer",
                columns: table => new
                {
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeverandorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Varenummer = table.Column<string>(type: "TEXT", nullable: true),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: true),
                    ErStandard = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentLeverandorer", x => new { x.ComponentId, x.LeverandorId });
                    table.ForeignKey(
                        name: "FK_ComponentLeverandorer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentLeverandorer_Leverandorer_LeverandorId",
                        column: x => x.LeverandorId,
                        principalTable: "Leverandorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentLeverandorer_LeverandorId",
                table: "ComponentLeverandorer",
                column: "LeverandorId");

            // Bakgrunnsfyller det nye leverandørregisteret fra det eksisterende
            // frittekst-feltet Components.Leverandor: én Leverandor-rad pr.
            // distinkt navn, og en "standard"-kobling for hver vare til sin
            // egen (opprinnelige) leverandør - se brukerens krav om at "alle
            // varer som importeres skal standard være tilknyttet seg selv".
            migrationBuilder.Sql(
                """
                INSERT INTO Leverandorer (Navn)
                SELECT DISTINCT TRIM(Leverandor) FROM Components
                WHERE Leverandor IS NOT NULL AND TRIM(Leverandor) <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO ComponentLeverandorer (ComponentId, LeverandorId, Varenummer, Pris, ErStandard)
                SELECT c.Id, l.Id, c.Produktkode, c.PrisNetto, 1
                FROM Components c
                JOIN Leverandorer l ON TRIM(l.Navn) = TRIM(c.Leverandor)
                WHERE c.Leverandor IS NOT NULL AND TRIM(c.Leverandor) <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentLeverandorer");

            migrationBuilder.DropTable(
                name: "Leverandorer");
        }
    }
}
