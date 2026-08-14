using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DoorEnvironmentDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoorEnvironmentDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoorEnvironmentDocuments", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DoorEnvironmentDocuments",
                columns: new[] { "Id", "FileName", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 1, "dormiljo-1.docx", "Dørmiljø 1", 1 },
                    { 2, "dormiljo-2.docx", "Dørmiljø 2", 2 },
                    { 3, "dormiljo-3.docx", "Dørmiljø 3", 3 },
                    { 4, "dormiljo-4.docx", "Dørmiljø 4", 4 },
                    { 5, "dormiljo-5.docx", "Dørmiljø 5", 5 },
                    { 6, "dormiljo-6.docx", "Dørmiljø 6", 6 },
                    { 7, "dormiljo-7.docx", "Dørmiljø 7", 7 },
                    { 8, "dormiljo-8.docx", "Dørmiljø 8", 8 },
                    { 9, "dormiljo-9.docx", "Dørmiljø 9", 9 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoorEnvironmentDocuments");
        }
    }
}
