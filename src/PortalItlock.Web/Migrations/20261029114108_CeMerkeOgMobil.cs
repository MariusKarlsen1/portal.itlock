using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeMerkeOgMobil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BruksanvisningOverlevert",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bruksomrade",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarepunkterJson",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KundeOpplaert",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LoggbokLagt",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerkeToken",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ServiceavtaleTilbudt",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);


            migrationBuilder.CreateIndex(
                name: "IX_CeGodkjenninger_MerkeToken",
                table: "CeGodkjenninger",
                column: "MerkeToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CeGodkjenninger_MerkeToken",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "BruksanvisningOverlevert",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Bruksomrade",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "FarepunkterJson",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "KundeOpplaert",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "LoggbokLagt",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "MerkeToken",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ServiceavtaleTilbudt",
                table: "CeGodkjenninger");

        }
    }
}
