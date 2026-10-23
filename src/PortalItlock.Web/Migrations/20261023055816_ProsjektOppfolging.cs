using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProsjektOppfolging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BerorteFag",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FristforlengelseDager",
                table: "Tilbud",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Forbedre",
                table: "ProsjektEtterkalkyler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForsinkelseAndre",
                table: "ProsjektEtterkalkyler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForsinkelseEgne",
                table: "ProsjektEtterkalkyler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GikkBra",
                table: "ProsjektEtterkalkyler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GikkDarlig",
                table: "ProsjektEtterkalkyler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BankgarantiBelop",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankgarantiTil",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Indeksregulering",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MontasjestartDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverleveringsfristDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResterendeTimer",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TilbudsfristDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnsattTimekost",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Arslonn = table.Column<decimal>(type: "TEXT", nullable: false),
                    SosialeProsent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Overhead = table.Column<decimal>(type: "TEXT", nullable: false),
                    FakturerbareTimer = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnsattTimekost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnsattTimekost_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektAkontoer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nr = table.Column<int>(type: "INTEGER", nullable: false),
                    Milepael = table.Column<string>(type: "TEXT", nullable: false),
                    Prosent = table.Column<decimal>(type: "TEXT", nullable: true),
                    Belop = table.Column<decimal>(type: "TEXT", nullable: true),
                    FremdriftProsent = table.Column<int>(type: "INTEGER", nullable: true),
                    PlanlagtDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SendtDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SendtBelop = table.Column<decimal>(type: "TEXT", nullable: true),
                    TripletexOrdreId = table.Column<int>(type: "INTEGER", nullable: true),
                    TripletexOrdreNummer = table.Column<string>(type: "TEXT", nullable: true),
                    TripletexFeil = table.Column<string>(type: "TEXT", nullable: true),
                    BetaltDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektAkontoer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektAkontoer_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektBudsjettJusteringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Post = table.Column<int>(type: "INTEGER", nullable: false),
                    Belop = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektBudsjettJusteringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektBudsjettJusteringer_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektplanAktiviteter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fase = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Rolle = table.Column<string>(type: "TEXT", nullable: true),
                    DelFra = table.Column<decimal>(type: "TEXT", nullable: false),
                    DelTil = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektplanAktiviteter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektplanAktiviteter_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektRoller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rolle = table.Column<string>(type: "TEXT", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektRoller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektRoller_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProsjektRoller_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektplanOppgaver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AktivitetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rolle = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    Endret = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndretAv = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektplanOppgaver", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektplanOppgaver_ProsjektplanAktiviteter_AktivitetId",
                        column: x => x.AktivitetId,
                        principalTable: "ProsjektplanAktiviteter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnsattTimekost_BrukerId",
                table: "AnsattTimekost",
                column: "BrukerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektAkontoer_ProsjektId",
                table: "ProsjektAkontoer",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektBudsjettJusteringer_ProsjektId_Post",
                table: "ProsjektBudsjettJusteringer",
                columns: new[] { "ProsjektId", "Post" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektplanAktiviteter_ProsjektId",
                table: "ProsjektplanAktiviteter",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektplanOppgaver_AktivitetId",
                table: "ProsjektplanOppgaver",
                column: "AktivitetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektRoller_BrukerId",
                table: "ProsjektRoller",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektRoller_ProsjektId_Rolle",
                table: "ProsjektRoller",
                columns: new[] { "ProsjektId", "Rolle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnsattTimekost");

            migrationBuilder.DropTable(
                name: "ProsjektAkontoer");

            migrationBuilder.DropTable(
                name: "ProsjektBudsjettJusteringer");

            migrationBuilder.DropTable(
                name: "ProsjektplanOppgaver");

            migrationBuilder.DropTable(
                name: "ProsjektRoller");

            migrationBuilder.DropTable(
                name: "ProsjektplanAktiviteter");

            migrationBuilder.DropColumn(
                name: "BerorteFag",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "FristforlengelseDager",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "Forbedre",
                table: "ProsjektEtterkalkyler");

            migrationBuilder.DropColumn(
                name: "ForsinkelseAndre",
                table: "ProsjektEtterkalkyler");

            migrationBuilder.DropColumn(
                name: "ForsinkelseEgne",
                table: "ProsjektEtterkalkyler");

            migrationBuilder.DropColumn(
                name: "GikkBra",
                table: "ProsjektEtterkalkyler");

            migrationBuilder.DropColumn(
                name: "GikkDarlig",
                table: "ProsjektEtterkalkyler");

            migrationBuilder.DropColumn(
                name: "BankgarantiBelop",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "BankgarantiTil",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "Indeksregulering",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "MontasjestartDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "OverleveringsfristDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ResterendeTimer",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "StartDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "TilbudsfristDato",
                table: "Prosjekter");
        }
    }
}
