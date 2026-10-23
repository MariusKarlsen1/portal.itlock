using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class FremdriftsplanUtenTilbudsfase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TilbudsfristDato",
                table: "Prosjekter");

            // Planen starter nå når prosjektet er registrert. Tilbuds- og
            // kalkulasjonsfasen (fase 0) fjernes fra planer som allerede er
            // laget, og de andre fasene flyttes ett nummer ned.
            migrationBuilder.Sql("DELETE FROM ProsjektplanOppgaver WHERE AktivitetId IN (SELECT Id FROM ProsjektplanAktiviteter WHERE Fase = 0);");
            migrationBuilder.Sql("DELETE FROM ProsjektplanAktiviteter WHERE Fase = 0;");
            migrationBuilder.Sql("UPDATE ProsjektplanAktiviteter SET Fase = Fase - 1 WHERE Fase > 0;");
            migrationBuilder.Sql("DELETE FROM ProsjektRoller WHERE Rolle IN ('Kalkulatør', 'Servicesjef');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TilbudsfristDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);
        }
    }
}
