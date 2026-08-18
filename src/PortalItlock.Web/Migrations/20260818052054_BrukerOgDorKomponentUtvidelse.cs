using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BrukerOgDorKomponentUtvidelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arbeidsordre_Montorer_AnsvarligMontorId",
                table: "Arbeidsordre");

            migrationBuilder.DropForeignKey(
                name: "FK_Timeregistreringer_Montorer_MontorId",
                table: "Timeregistreringer");

            migrationBuilder.RenameTable(
                name: "Montorer",
                newName: "Brukere");

            migrationBuilder.AddColumn<string>(
                name: "Epost",
                table: "Brukere",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Brukere",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stilling",
                table: "Brukere",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rolle",
                table: "Brukere",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "Enhet",
                table: "DorKomponenter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MontertAvBrukerId",
                table: "DorKomponenter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DorKomponenter_MontertAvBrukerId",
                table: "DorKomponenter",
                column: "MontertAvBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Arbeidsordre_Brukere_AnsvarligMontorId",
                table: "Arbeidsordre",
                column: "AnsvarligMontorId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DorKomponenter_Brukere_MontertAvBrukerId",
                table: "DorKomponenter",
                column: "MontertAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Timeregistreringer_Brukere_MontorId",
                table: "Timeregistreringer",
                column: "MontorId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arbeidsordre_Brukere_AnsvarligMontorId",
                table: "Arbeidsordre");

            migrationBuilder.DropForeignKey(
                name: "FK_DorKomponenter_Brukere_MontertAvBrukerId",
                table: "DorKomponenter");

            migrationBuilder.DropForeignKey(
                name: "FK_Timeregistreringer_Brukere_MontorId",
                table: "Timeregistreringer");

            migrationBuilder.DropIndex(
                name: "IX_DorKomponenter_MontertAvBrukerId",
                table: "DorKomponenter");

            migrationBuilder.DropColumn(
                name: "Enhet",
                table: "DorKomponenter");

            migrationBuilder.DropColumn(
                name: "MontertAvBrukerId",
                table: "DorKomponenter");

            migrationBuilder.DropColumn(
                name: "Epost",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "Stilling",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "Rolle",
                table: "Brukere");

            migrationBuilder.RenameTable(
                name: "Brukere",
                newName: "Montorer");

            migrationBuilder.AddForeignKey(
                name: "FK_Arbeidsordre_Montorer_AnsvarligMontorId",
                table: "Arbeidsordre",
                column: "AnsvarligMontorId",
                principalTable: "Montorer",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Timeregistreringer_Montorer_MontorId",
                table: "Timeregistreringer",
                column: "MontorId",
                principalTable: "Montorer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
