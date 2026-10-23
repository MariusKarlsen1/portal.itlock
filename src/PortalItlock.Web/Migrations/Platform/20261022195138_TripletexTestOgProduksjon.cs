using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class TripletexTestOgProduksjon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TripletexBrukTest",
                table: "TenantInnstillinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TripletexTestEmployeeTokenKryptert",
                table: "TenantInnstillinger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripletexTestConsumerTokenKryptert",
                table: "PlattformInnstillinger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripletexTestEmployeeTokenKryptert",
                table: "PlattformInnstillinger",
                type: "TEXT",
                nullable: true);

            // Alt som var satt opp før, var mot Tripletex sitt testmiljø: nøkler
            // merket som test flyttes til testfeltene, og organisasjonenes
            // ansattnøkler flyttes dit så lenge det ikke finnes et consumer
            // token for produksjon. Organisasjonene bruker da testmiljøet.
            migrationBuilder.Sql(@"
UPDATE PlattformInnstillinger
SET TripletexTestConsumerTokenKryptert = TripletexConsumerTokenKryptert,
    TripletexTestEmployeeTokenKryptert = TripletexEmployeeTokenKryptert,
    TripletexConsumerTokenKryptert = NULL,
    TripletexEmployeeTokenKryptert = NULL
WHERE TripletexTest = 1;");
            migrationBuilder.Sql(@"
UPDATE TenantInnstillinger
SET TripletexTestEmployeeTokenKryptert = TripletexEmployeeTokenKryptert,
    TripletexEmployeeTokenKryptert = NULL
WHERE TripletexEmployeeTokenKryptert IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM PlattformInnstillinger WHERE TripletexConsumerTokenKryptert IS NOT NULL);");
            migrationBuilder.Sql(@"
UPDATE TenantInnstillinger
SET TripletexBrukTest = 1
WHERE NOT EXISTS (SELECT 1 FROM PlattformInnstillinger WHERE TripletexConsumerTokenKryptert IS NOT NULL);");
            migrationBuilder.Sql(@"
UPDATE PlattformInnstillinger
SET TripletexTest = 1
WHERE TripletexConsumerTokenKryptert IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TripletexBrukTest",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "TripletexTestEmployeeTokenKryptert",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "TripletexTestConsumerTokenKryptert",
                table: "PlattformInnstillinger");

            migrationBuilder.DropColumn(
                name: "TripletexTestEmployeeTokenKryptert",
                table: "PlattformInnstillinger");
        }
    }
}
