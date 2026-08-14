using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ComponentPricingAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_ComponentTypes_ComponentTypeId",
                table: "Components");

            migrationBuilder.AlterColumn<int>(
                name: "ComponentTypeId",
                table: "Components",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "Aktiv",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Leverandor",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrisNetto",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrisVeiledende",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Varegruppe",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Components_ComponentTypes_ComponentTypeId",
                table: "Components",
                column: "ComponentTypeId",
                principalTable: "ComponentTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_ComponentTypes_ComponentTypeId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Aktiv",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Leverandor",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "PrisNetto",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "PrisVeiledende",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Varegruppe",
                table: "Components");

            migrationBuilder.AlterColumn<int>(
                name: "ComponentTypeId",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Components_ComponentTypes_ComponentTypeId",
                table: "Components",
                column: "ComponentTypeId",
                principalTable: "ComponentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
