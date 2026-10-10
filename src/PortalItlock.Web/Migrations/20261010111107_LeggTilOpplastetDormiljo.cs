using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilOpplastetDormiljo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "DoorEnvironmentDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "DoorEnvironmentDocuments",
                type: "BLOB",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "DoorEnvironmentDocuments",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ContentType", "Data" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "DoorEnvironmentDocuments");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "DoorEnvironmentDocuments");
        }
    }
}
