using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeDorIdOgProdusentUtvidelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AMal",
                table: "DorIdMaler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BMal",
                table: "DorIdMaler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CeDorblad",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CeGlasstykkelse",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dorkonstruksjon",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FargeDorblad",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FargeKarm",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GlassIDor",
                table: "DorIdMaler",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Karmkonstruksjon",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Karmtype",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Merknad",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sparkeplate",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Terskel",
                table: "DorIdMaler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentAdresse",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentLand",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentOrgnr",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentPostnr",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentSted",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AMal",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BMal",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dorblad",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FargeDorblad",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FargeKarm",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Glasstykkelse",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Karmtype",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentAdresse",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentLand",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentOrgnr",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentPostnr",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdusentSted",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sparkeplate",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Terskeltype",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AMal",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "BMal",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "CeDorblad",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "CeGlasstykkelse",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "Dorkonstruksjon",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "FargeDorblad",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "FargeKarm",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "GlassIDor",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "Karmkonstruksjon",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "Karmtype",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "Merknad",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "Sparkeplate",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "Terskel",
                table: "DorIdMaler");

            migrationBuilder.DropColumn(
                name: "ProdusentAdresse",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ProdusentLand",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ProdusentOrgnr",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ProdusentPostnr",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ProdusentSted",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "AMal",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "BMal",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Dorblad",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "FargeDorblad",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "FargeKarm",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Glasstykkelse",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Karmtype",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ProdusentAdresse",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ProdusentLand",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ProdusentOrgnr",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ProdusentPostnr",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ProdusentSted",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Sparkeplate",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Terskeltype",
                table: "CeGodkjenninger");
        }
    }
}
