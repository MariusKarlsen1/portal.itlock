using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ForesporselSaksbehandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Foresporsler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnsvarligBrukerId",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArbeidsordreId",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Frist",
                table: "Foresporsler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kategori",
                table: "Foresporsler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Prioritet",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Foresporsler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TilbudId",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideresendtAv",
                table: "Foresporsler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErSystem",
                table: "ForesporselNotater",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Foresporsler_AnsvarligBrukerId",
                table: "Foresporsler",
                column: "AnsvarligBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_Foresporsler_KundeId",
                table: "Foresporsler",
                column: "KundeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Foresporsler_Brukere_AnsvarligBrukerId",
                table: "Foresporsler",
                column: "AnsvarligBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Foresporsler_Kunder_KundeId",
                table: "Foresporsler",
                column: "KundeId",
                principalTable: "Kunder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Status, ansvarlig, kunde, frist og prioritet flyttes fra den skjulte
            // ticketen (nyeste per forespørsel) til selve forespørselen. Statusene har
            // samme rekkefølge (Venter godkjenning blir Venter på kunde, Lukket blir
            // Ferdig), og Kritisk blir Høy.
            migrationBuilder.Sql(@"UPDATE ""Foresporsler"" SET ""Status"" = (SELECT t.""Status"" FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"" ORDER BY t.""OpprettetDato"" DESC LIMIT 1), ""AnsvarligBrukerId"" = (SELECT t.""AnsvarligBrukerId"" FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"" ORDER BY t.""OpprettetDato"" DESC LIMIT 1), ""KundeId"" = (SELECT t.""KundeId"" FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"" ORDER BY t.""OpprettetDato"" DESC LIMIT 1), ""Frist"" = (SELECT t.""SlaFrist"" FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"" ORDER BY t.""OpprettetDato"" DESC LIMIT 1), ""ArbeidsordreId"" = (SELECT t.""ArbeidsordreId"" FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"" ORDER BY t.""OpprettetDato"" DESC LIMIT 1), ""Prioritet"" = MIN(2, (SELECT t.""Prioritet"" FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"" ORDER BY t.""OpprettetDato"" DESC LIMIT 1)) WHERE EXISTS (SELECT 1 FROM ""Tickets"" t WHERE t.""ForesporselId"" = ""Foresporsler"".""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Foresporsler_Brukere_AnsvarligBrukerId",
                table: "Foresporsler");

            migrationBuilder.DropForeignKey(
                name: "FK_Foresporsler_Kunder_KundeId",
                table: "Foresporsler");

            migrationBuilder.DropIndex(
                name: "IX_Foresporsler_AnsvarligBrukerId",
                table: "Foresporsler");

            migrationBuilder.DropIndex(
                name: "IX_Foresporsler_KundeId",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "AnsvarligBrukerId",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "ArbeidsordreId",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "Frist",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "Prioritet",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "TilbudId",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "VideresendtAv",
                table: "Foresporsler");

            migrationBuilder.DropColumn(
                name: "ErSystem",
                table: "ForesporselNotater");
        }
    }
}
