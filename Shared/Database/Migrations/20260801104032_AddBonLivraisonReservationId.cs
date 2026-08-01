using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBonLivraisonReservationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "BonsLivraison",
                type: "INTEGER",
                nullable: true);

            // Backfill from existing Reservation → BL links; clear legacy auto-generated notes.
            migrationBuilder.Sql("""
                UPDATE "BonsLivraison"
                SET "ReservationId" = (
                    SELECT r."Id" FROM "Reservations" AS r
                    WHERE r."BonLivraisonId" = "BonsLivraison"."Id"
                    LIMIT 1
                )
                WHERE "ReservationId" IS NULL
                  AND EXISTS (
                      SELECT 1 FROM "Reservations" AS r
                      WHERE r."BonLivraisonId" = "BonsLivraison"."Id"
                  );

                UPDATE "BonsLivraison"
                SET "Note" = ''
                WHERE "ReservationId" IS NOT NULL
                  AND EXISTS (
                      SELECT 1 FROM "Reservations" AS r
                      WHERE r."Id" = "BonsLivraison"."ReservationId"
                        AND (
                            TRIM("BonsLivraison"."Note") = 'Réservation ' || r."Numero"
                            OR TRIM("BonsLivraison"."Note") = 'حجز ' || r."Numero"
                        )
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_ReservationId",
                table: "BonsLivraison",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_Reservations_ReservationId",
                table: "BonsLivraison",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_Reservations_ReservationId",
                table: "BonsLivraison");

            migrationBuilder.DropIndex(
                name: "IX_BonsLivraison_ReservationId",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "BonsLivraison");
        }
    }
}
