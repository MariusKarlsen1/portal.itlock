using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DorIdFloyer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ToFloyet",
                table: "DorIdMaler",
                type: "INTEGER",
                nullable: true);

            // Dør ID: fritekst-«Dørtype» (Tre, Stål, Aluminium ...) flyttes til Dørkonstruksjon der den er tom.
            migrationBuilder.Sql(@"
                UPDATE DorIdMaler SET Dorkonstruksjon = TRIM(Dortype)
                WHERE (Dorkonstruksjon IS NULL OR TRIM(Dorkonstruksjon) = '') AND Dortype IS NOT NULL AND TRIM(Dortype) <> '';");

            // Dører med bare «B x H» (f.eks. 1000x2100) får bredde og høyde fylt ut.
            migrationBuilder.Sql(@"
                UPDATE Dorer SET
                    Bredde = CASE WHEN Bredde IS NULL THEN CAST(TRIM(substr(lower(replace(BxH, '×', 'x')), 1, instr(lower(replace(BxH, '×', 'x')), 'x') - 1)) AS INTEGER) ELSE Bredde END,
                    Hoyde = CASE WHEN Hoyde IS NULL THEN CAST(TRIM(substr(lower(replace(BxH, '×', 'x')), instr(lower(replace(BxH, '×', 'x')), 'x') + 1)) AS INTEGER) ELSE Hoyde END
                WHERE (Bredde IS NULL OR Hoyde IS NULL)
                  AND instr(lower(replace(BxH, '×', 'x')), 'x') > 1
                  AND TRIM(substr(lower(replace(BxH, '×', 'x')), 1, instr(lower(replace(BxH, '×', 'x')), 'x') - 1)) GLOB '[0-9]*'
                  AND TRIM(substr(lower(replace(BxH, '×', 'x')), instr(lower(replace(BxH, '×', 'x')), 'x') + 1)) GLOB '[0-9]*';");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToFloyet",
                table: "DorIdMaler");

        }
    }
}
