using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingsFigurerOgPunkter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "BildeData",
                table: "KoblingsSymboler",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<string>(
                name: "BildeContentType",
                table: "KoblingsSymboler",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "ElementType",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Farge",
                table: "KoblingsSymboler",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FontStorrelse",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Fylt",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Strokbredde",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tekst",
                table: "KoblingsSymboler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KoblingsPunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KoblingsSymbolId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelX = table.Column<double>(type: "REAL", nullable: false),
                    RelY = table.Column<double>(type: "REAL", nullable: false),
                    Storrelse = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsPunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KoblingsPunkter_KoblingsSymboler_KoblingsSymbolId",
                        column: x => x.KoblingsSymbolId,
                        principalTable: "KoblingsSymboler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsPunkter_KoblingsSymbolId",
                table: "KoblingsPunkter",
                column: "KoblingsSymbolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KoblingsPunkter");

            migrationBuilder.DropColumn(
                name: "ElementType",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "Farge",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "FontStorrelse",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "Fylt",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "Strokbredde",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "Tekst",
                table: "KoblingsSymboler");

            migrationBuilder.AlterColumn<byte[]>(
                name: "BildeData",
                table: "KoblingsSymboler",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BildeContentType",
                table: "KoblingsSymboler",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
