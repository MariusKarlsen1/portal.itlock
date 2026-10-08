using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilAutomatiskSkyvedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RequirementValues",
                columns: new[] { "Id", "Kode", "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 32, null, 4, 6, "Automatisk skyvedør" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 32);
        }
    }
}
