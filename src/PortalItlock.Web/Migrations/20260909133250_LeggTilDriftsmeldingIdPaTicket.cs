using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDriftsmeldingIdPaTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriftsmeldingId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DriftsmeldingId",
                table: "Tickets",
                column: "DriftsmeldingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Driftsmeldinger_DriftsmeldingId",
                table: "Tickets",
                column: "DriftsmeldingId",
                principalTable: "Driftsmeldinger",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Driftsmeldinger_DriftsmeldingId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DriftsmeldingId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DriftsmeldingId",
                table: "Tickets");
        }
    }
}
