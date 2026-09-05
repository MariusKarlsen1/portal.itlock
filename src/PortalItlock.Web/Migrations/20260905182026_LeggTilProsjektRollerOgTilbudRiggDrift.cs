using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilProsjektRollerOgTilbudRiggDrift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RiggDriftProsent",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnsvarligMontorId",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProsjektlederId",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prosjekter_AnsvarligMontorId",
                table: "Prosjekter",
                column: "AnsvarligMontorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prosjekter_ProsjektlederId",
                table: "Prosjekter",
                column: "ProsjektlederId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prosjekter_Brukere_AnsvarligMontorId",
                table: "Prosjekter",
                column: "AnsvarligMontorId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Prosjekter_Brukere_ProsjektlederId",
                table: "Prosjekter",
                column: "ProsjektlederId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prosjekter_Brukere_AnsvarligMontorId",
                table: "Prosjekter");

            migrationBuilder.DropForeignKey(
                name: "FK_Prosjekter_Brukere_ProsjektlederId",
                table: "Prosjekter");

            migrationBuilder.DropIndex(
                name: "IX_Prosjekter_AnsvarligMontorId",
                table: "Prosjekter");

            migrationBuilder.DropIndex(
                name: "IX_Prosjekter_ProsjektlederId",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "RiggDriftProsent",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "AnsvarligMontorId",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ProsjektlederId",
                table: "Prosjekter");
        }
    }
}
