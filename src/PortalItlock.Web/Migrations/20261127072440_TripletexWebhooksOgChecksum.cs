using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TripletexWebhooksOgChecksum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProduktDigest",
                table: "TripletexSyncTilstand",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SistFullSynkUtc",
                table: "TripletexSyncTilstand",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookAdresse",
                table: "TripletexSyncTilstand",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WebhookAktiv",
                table: "TripletexSyncTilstand",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WebhookFeil",
                table: "TripletexSyncTilstand",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookHemmelighet",
                table: "TripletexSyncTilstand",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProduktDigest",
                table: "TripletexSyncTilstand");

            migrationBuilder.DropColumn(
                name: "SistFullSynkUtc",
                table: "TripletexSyncTilstand");

            migrationBuilder.DropColumn(
                name: "WebhookAdresse",
                table: "TripletexSyncTilstand");

            migrationBuilder.DropColumn(
                name: "WebhookAktiv",
                table: "TripletexSyncTilstand");

            migrationBuilder.DropColumn(
                name: "WebhookFeil",
                table: "TripletexSyncTilstand");

            migrationBuilder.DropColumn(
                name: "WebhookHemmelighet",
                table: "TripletexSyncTilstand");
        }
    }
}
