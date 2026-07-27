using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationBonLivraisonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BonLivraisonId",
                table: "Locations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_BonLivraisonId",
                table: "Locations",
                column: "BonLivraisonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_BonsLivraison_BonLivraisonId",
                table: "Locations",
                column: "BonLivraisonId",
                principalTable: "BonsLivraison",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_BonsLivraison_BonLivraisonId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_BonLivraisonId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "BonLivraisonId",
                table: "Locations");
        }
    }
}
