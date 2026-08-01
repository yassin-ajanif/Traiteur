using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationProduitRetours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationProduitRetours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReservationProduitLigneId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateRetour = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationProduitRetours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationProduitRetours_ReservationProduitLignes_ReservationProduitLigneId",
                        column: x => x.ReservationProduitLigneId,
                        principalTable: "ReservationProduitLignes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationProduitRetours_DateRetour",
                table: "ReservationProduitRetours",
                column: "DateRetour");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationProduitRetours_ReservationProduitLigneId",
                table: "ReservationProduitRetours",
                column: "ReservationProduitLigneId");

            // Seed one history row per existing returned qty so history matches QuantiteRetournee.
            migrationBuilder.Sql("""
                INSERT INTO "ReservationProduitRetours"
                    ("ReservationProduitLigneId", "DateRetour", "Quantite", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId")
                SELECT
                    l."Id",
                    COALESCE(r."DateRetourEffective", r."Date", datetime('now')),
                    l."QuantiteRetournee",
                    '',
                    datetime('now'),
                    datetime('now'),
                    NULL
                FROM "ReservationProduitLignes" AS l
                INNER JOIN "Reservations" AS r ON r."Id" = l."ReservationId"
                WHERE l."QuantiteRetournee" > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationProduitRetours");
        }
    }
}
