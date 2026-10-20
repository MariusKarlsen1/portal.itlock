using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Lagerstyring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Snittkost",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lagre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    AnsvarligBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    Opprettet = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lagre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lagre_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LagerBevegelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    KostPerStk = table.Column<double>(type: "REAL", nullable: false),
                    FlyttId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: true),
                    VaretellingId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrukerNavn = table.Column<string>(type: "TEXT", nullable: true),
                    Notat = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LagerBevegelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LagerBevegelser_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LagerBevegelser_Lagre_LagerId",
                        column: x => x.LagerId,
                        principalTable: "Lagre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VareLagerplasser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hylle = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VareLagerplasser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VareLagerplasser_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VareLagerplasser_Lagre_LagerId",
                        column: x => x.LagerId,
                        principalTable: "Lagre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Varetellinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Startet = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Godkjent = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartetAv = table.Column<string>(type: "TEXT", nullable: true),
                    GodkjentAv = table.Column<string>(type: "TEXT", nullable: true),
                    Notat = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Varetellinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Varetellinger_Lagre_LagerId",
                        column: x => x.LagerId,
                        principalTable: "Lagre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaretellingLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VaretellingId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Registrert = table.Column<int>(type: "INTEGER", nullable: false),
                    Talt = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaretellingLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaretellingLinjer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaretellingLinjer_Varetellinger_VaretellingId",
                        column: x => x.VaretellingId,
                        principalTable: "Varetellinger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LagerBevegelser_ComponentId_LagerId",
                table: "LagerBevegelser",
                columns: new[] { "ComponentId", "LagerId" });

            migrationBuilder.CreateIndex(
                name: "IX_LagerBevegelser_LagerId",
                table: "LagerBevegelser",
                column: "LagerId");

            migrationBuilder.CreateIndex(
                name: "IX_LagerBevegelser_Tidspunkt",
                table: "LagerBevegelser",
                column: "Tidspunkt");

            migrationBuilder.CreateIndex(
                name: "IX_Lagre_ProsjektId",
                table: "Lagre",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_VareLagerplasser_ComponentId_LagerId",
                table: "VareLagerplasser",
                columns: new[] { "ComponentId", "LagerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VareLagerplasser_LagerId",
                table: "VareLagerplasser",
                column: "LagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Varetellinger_LagerId",
                table: "Varetellinger",
                column: "LagerId");

            migrationBuilder.CreateIndex(
                name: "IX_VaretellingLinjer_ComponentId",
                table: "VaretellingLinjer",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_VaretellingLinjer_VaretellingId",
                table: "VaretellingLinjer",
                column: "VaretellingId");

            // Hovedlager + startbeholdning: antallet fra den gamle
            // lageroversikten (Components.Lagerantall) blir første bevegelse,
            // så beholdningen er den samme som før og har historikk fra start.
            migrationBuilder.Sql(
                "INSERT INTO Lagre (Navn, Type, Aktiv, Opprettet) VALUES ('Hovedlager', 0, 1, datetime('now', 'localtime'));");
            migrationBuilder.Sql(
                "INSERT INTO LagerBevegelser (Tidspunkt, ComponentId, LagerId, Type, Antall, KostPerStk, Notat) " +
                "SELECT datetime('now', 'localtime'), c.Id, (SELECT Id FROM Lagre WHERE Type = 0 ORDER BY Id LIMIT 1), 0, c.Lagerantall, " +
                "COALESCE(CAST(c.PrisNetto AS REAL), 0), 'Startbeholdning fra tidligere lageroversikt' " +
                "FROM Components c WHERE c.ILagerstyring = 1 AND c.Lagerantall <> 0;");
            migrationBuilder.Sql(
                "UPDATE Components SET Snittkost = PrisNetto WHERE ILagerstyring = 1 AND PrisNetto IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LagerBevegelser");

            migrationBuilder.DropTable(
                name: "VareLagerplasser");

            migrationBuilder.DropTable(
                name: "VaretellingLinjer");

            migrationBuilder.DropTable(
                name: "Varetellinger");

            migrationBuilder.DropTable(
                name: "Lagre");

            migrationBuilder.DropColumn(
                name: "Snittkost",
                table: "Components");
        }
    }
}
