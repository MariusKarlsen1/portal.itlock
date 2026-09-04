using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeDordesignForenklingOgGlassType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "EnergiKlasse",
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
                name: "Serviceadresse",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Sparkeplate",
                table: "CeGodkjenninger");

            migrationBuilder.RenameColumn(
                name: "Terskeltype",
                table: "CeGodkjenninger",
                newName: "TypeAvGlass");

            migrationBuilder.AlterColumn<string>(
                name: "GlassIDor",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TypeAvGlass",
                table: "CeGodkjenninger",
                newName: "Terskeltype");

            migrationBuilder.AlterColumn<bool>(
                name: "GlassIDor",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

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
                name: "EnergiKlasse",
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
                name: "Serviceadresse",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sparkeplate",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);
        }
    }
}
