using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class MontorKontrollTripletexKunde : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TripletexMaSendes",
                table: "Kunder",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MontorFerdigmeldtAvBrukerId",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MontorFerdigmeldtAvNavn",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MontorFerdigmeldtDato",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TripletexMaSendes",
                table: "Kunder");

            migrationBuilder.DropColumn(
                name: "MontorFerdigmeldtAvBrukerId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "MontorFerdigmeldtAvNavn",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "MontorFerdigmeldtDato",
                table: "Arbeidsordre");
        }
    }
}
