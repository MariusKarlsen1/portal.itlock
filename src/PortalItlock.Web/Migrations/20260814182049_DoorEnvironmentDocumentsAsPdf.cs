using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DoorEnvironmentDocumentsAsPdf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 1,
                column: "FileName",
                value: "dormiljo-1.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 2,
                column: "FileName",
                value: "dormiljo-2.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 3,
                column: "FileName",
                value: "dormiljo-3.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 4,
                column: "FileName",
                value: "dormiljo-4.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 5,
                column: "FileName",
                value: "dormiljo-5.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 6,
                column: "FileName",
                value: "dormiljo-6.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 7,
                column: "FileName",
                value: "dormiljo-7.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 8,
                column: "FileName",
                value: "dormiljo-8.pdf");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 9,
                column: "FileName",
                value: "dormiljo-9.pdf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 1,
                column: "FileName",
                value: "dormiljo-1.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 2,
                column: "FileName",
                value: "dormiljo-2.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 3,
                column: "FileName",
                value: "dormiljo-3.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 4,
                column: "FileName",
                value: "dormiljo-4.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 5,
                column: "FileName",
                value: "dormiljo-5.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 6,
                column: "FileName",
                value: "dormiljo-6.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 7,
                column: "FileName",
                value: "dormiljo-7.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 8,
                column: "FileName",
                value: "dormiljo-8.docx");

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 9,
                column: "FileName",
                value: "dormiljo-9.docx");
        }
    }
}
