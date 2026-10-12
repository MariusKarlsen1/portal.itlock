using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsSkjemaHvitFlateUtenRutenett : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tegneflaten skal være hvit uten rutenett. Kolonnen ble lagt til
            // med defaultValue true, så både standarden og de skjemaene som
            // allerede finnes må snus - ellers ville bare helt nye skjema fått
            // den nye flaten.
            migrationBuilder.AlterColumn<bool>(
                name: "VisRutenett",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.Sql("UPDATE \"KoblingsSkjemaer\" SET \"VisRutenett\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "VisRutenett",
                table: "KoblingsSkjemaer",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);
        }
    }
}
