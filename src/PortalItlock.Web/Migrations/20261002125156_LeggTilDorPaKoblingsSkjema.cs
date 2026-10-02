using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDorPaKoblingsSkjema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DorId",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.InsertData(
                table: "KoblingsKategorier",
                columns: new[] { "Id", "Navn", "Rekkefolge" },
                values: new object[] { 4, "Prinsippskisser", 4 });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsSkjemaer_DorId",
                table: "KoblingsSkjemaer",
                column: "DorId");

            migrationBuilder.AddForeignKey(
                name: "FK_KoblingsSkjemaer_Dorer_DorId",
                table: "KoblingsSkjemaer",
                column: "DorId",
                principalTable: "Dorer",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KoblingsSkjemaer_Dorer_DorId",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropIndex(
                name: "IX_KoblingsSkjemaer_DorId",
                table: "KoblingsSkjemaer");

            migrationBuilder.DeleteData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "DorId",
                table: "KoblingsSkjemaer");
        }
    }
}
