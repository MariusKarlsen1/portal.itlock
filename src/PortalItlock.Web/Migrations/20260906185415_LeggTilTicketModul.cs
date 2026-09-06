using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTicketModul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    KundeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Lokasjon = table.Column<string>(type: "TEXT", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: true),
                    Board = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    ProduktAnlegg = table.Column<string>(type: "TEXT", nullable: true),
                    Prioritet = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Sla = table.Column<string>(type: "TEXT", nullable: true),
                    AnsvarligBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LukketDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GodkjentAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ForesporselId = table.Column<int>(type: "INTEGER", nullable: true),
                    ServicehenvendelseId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Brukere_AnsvarligBrukerId",
                        column: x => x.AnsvarligBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Brukere_GodkjentAvBrukerId",
                        column: x => x.GodkjentAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Foresporsler_ForesporselId",
                        column: x => x.ForesporselId,
                        principalTable: "Foresporsler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Kunder_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Servicehenvendelser_ServicehenvendelseId",
                        column: x => x.ServicehenvendelseId,
                        principalTable: "Servicehenvendelser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketHendelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    UtfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketHendelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketHendelser_Brukere_UtfortAvBrukerId",
                        column: x => x.UtfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketHendelser_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketMedia_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketHendelser_TicketId",
                table: "TicketHendelser",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHendelser_UtfortAvBrukerId",
                table: "TicketHendelser",
                column: "UtfortAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMedia_TicketId",
                table: "TicketMedia",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AnsvarligBrukerId",
                table: "Tickets",
                column: "AnsvarligBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ArbeidsordreId",
                table: "Tickets",
                column: "ArbeidsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ForesporselId",
                table: "Tickets",
                column: "ForesporselId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_GodkjentAvBrukerId",
                table: "Tickets",
                column: "GodkjentAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_KundeId",
                table: "Tickets",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProsjektId",
                table: "Tickets",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServicehenvendelseId",
                table: "Tickets",
                column: "ServicehenvendelseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketHendelser");

            migrationBuilder.DropTable(
                name: "TicketMedia");

            migrationBuilder.DropTable(
                name: "Tickets");
        }
    }
}
