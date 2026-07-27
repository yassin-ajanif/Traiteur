using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLocationStatuts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy: 0=Brouillon, 4=Annulée → En cours (1)
            migrationBuilder.Sql(
                """
                UPDATE Locations
                SET Statut = 1
                WHERE Statut NOT IN (1, 2, 3);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
