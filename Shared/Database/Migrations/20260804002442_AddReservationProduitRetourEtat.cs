using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationProduitRetourEtat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Etat",
                table: "ReservationProduitRetours",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "good");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReservationProduitRetours_Etat",
                table: "ReservationProduitRetours",
                sql: "\"Etat\" IN ('good', 'damaged', 'lost', 'to clean')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ReservationProduitRetours_Etat",
                table: "ReservationProduitRetours");

            migrationBuilder.DropColumn(
                name: "Etat",
                table: "ReservationProduitRetours");
        }
    }
}
