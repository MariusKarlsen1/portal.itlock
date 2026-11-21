using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KunderSendesTilTripletexEnGang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alle kunder sendes til Tripletex én gang, så kontaktpersoner,
            // fakturaepost og privatperson kommer med også på dem som var koblet fra før.
            migrationBuilder.Sql("UPDATE Kunder SET TripletexMaSendes = 1 WHERE Navn <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
