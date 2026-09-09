using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDorOgReklamasjonPaTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DorId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErReklamasjon",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DorId",
                table: "Tickets",
                column: "DorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Dorer_DorId",
                table: "Tickets",
                column: "DorId",
                principalTable: "Dorer",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Dorer_DorId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ErReklamasjon",
                table: "Tickets");
        }
    }
}
