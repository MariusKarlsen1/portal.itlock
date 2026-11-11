using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Snarveier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Snarveier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    LogoData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    LogoContentType = table.Column<string>(type: "TEXT", nullable: true),
                    StandardLogo = table.Column<string>(type: "TEXT", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    Endret = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snarveier", x => x.Id);
                });

            // Standardsnarveiene som lå fast i koden før - nå redigerbare under Admin.
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('Tripletex', 'https://tripletex.no/execute/login', 'tripletex.png', 0, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('dormakaba', 'https://portal-dormakaba.onelogin.com/', 'dormakaba.png', 1, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('Steplock', 'https://steplock.com/NO/no/login', 'steplock.ico', 2, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('ASSA ABLOY', 'https://www.assaabloy.com/no', 'assaabloy.png', 3, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('iLOQ', 'https://partners.iloq.com/login', 'iloq.png', 4, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('SALTO', 'https://partners.saltowecosystem.com/English/', 'salto.png', 5, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('Larry', 'https://commissioning.saltoks.com/', 'larry.webp', 6, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("INSERT INTO Snarveier (Navn, Url, StandardLogo, Rekkefolge, Endret) VALUES ('Carl Ras', 'https://www.carl-ras.dk/', 'carlras.png', 7, CURRENT_TIMESTAMP);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Snarveier");
        }
    }
}
