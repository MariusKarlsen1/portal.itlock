using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Prosjekter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dorer_Plantegninger_PlantegningId",
                table: "Dorer");

            migrationBuilder.DropForeignKey(
                name: "FK_Plantegninger_Nokkelsystemer_NokkelsystemId",
                table: "Plantegninger");

            migrationBuilder.RenameColumn(
                name: "NokkelsystemId",
                table: "Plantegninger",
                newName: "ProsjektId");

            migrationBuilder.RenameIndex(
                name: "IX_Plantegninger_NokkelsystemId",
                table: "Plantegninger",
                newName: "IX_Plantegninger_ProsjektId");

            migrationBuilder.AlterColumn<double>(
                name: "PosY",
                table: "Dorer",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "PosX",
                table: "Dorer",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<int>(
                name: "PlantegningId",
                table: "Dorer",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ProsjektId",
                table: "Dorer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Prosjekter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kunde = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Postnr = table.Column<string>(type: "TEXT", nullable: true),
                    Sted = table.Column<string>(type: "TEXT", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prosjekter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektVedlegg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektVedlegg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektVedlegg_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dorer_ProsjektId",
                table: "Dorer",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektVedlegg_ProsjektId",
                table: "ProsjektVedlegg",
                column: "ProsjektId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dorer_Plantegninger_PlantegningId",
                table: "Dorer",
                column: "PlantegningId",
                principalTable: "Plantegninger",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dorer_Prosjekter_ProsjektId",
                table: "Dorer",
                column: "ProsjektId",
                principalTable: "Prosjekter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plantegninger_Prosjekter_ProsjektId",
                table: "Plantegninger",
                column: "ProsjektId",
                principalTable: "Prosjekter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dorer_Plantegninger_PlantegningId",
                table: "Dorer");

            migrationBuilder.DropForeignKey(
                name: "FK_Dorer_Prosjekter_ProsjektId",
                table: "Dorer");

            migrationBuilder.DropForeignKey(
                name: "FK_Plantegninger_Prosjekter_ProsjektId",
                table: "Plantegninger");

            migrationBuilder.DropTable(
                name: "ProsjektVedlegg");

            migrationBuilder.DropTable(
                name: "Prosjekter");

            migrationBuilder.DropIndex(
                name: "IX_Dorer_ProsjektId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "ProsjektId",
                table: "Dorer");

            migrationBuilder.RenameColumn(
                name: "ProsjektId",
                table: "Plantegninger",
                newName: "NokkelsystemId");

            migrationBuilder.RenameIndex(
                name: "IX_Plantegninger_ProsjektId",
                table: "Plantegninger",
                newName: "IX_Plantegninger_NokkelsystemId");

            migrationBuilder.AlterColumn<double>(
                name: "PosY",
                table: "Dorer",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PosX",
                table: "Dorer",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantegningId",
                table: "Dorer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Dorer_Plantegninger_PlantegningId",
                table: "Dorer",
                column: "PlantegningId",
                principalTable: "Plantegninger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plantegninger_Nokkelsystemer_NokkelsystemId",
                table: "Plantegninger",
                column: "NokkelsystemId",
                principalTable: "Nokkelsystemer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
