using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations;

/// <inheritdoc />
public partial class RenameLocationToReservationAndSplitLignes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve header rows + IDs (stock movements reference OrigineId = reservation Id).
        migrationBuilder.Sql("ALTER TABLE Locations RENAME TO Reservations;");

        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Locations_BonLivraisonId;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Locations_ClientId;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Locations_FactureId;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Locations_Numero;");

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_BonLivraisonId",
            table: "Reservations",
            column: "BonLivraisonId");

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_ClientId",
            table: "Reservations",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_FactureId",
            table: "Reservations",
            column: "FactureId");

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_Numero",
            table: "Reservations",
            column: "Numero");

        migrationBuilder.CreateTable(
            name: "ReservationProduitLignes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ReservationId = table.Column<int>(type: "INTEGER", nullable: false),
                ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                Designation = table.Column<string>(type: "TEXT", nullable: false),
                Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                QuantiteRetournee = table.Column<decimal>(type: "TEXT", nullable: false),
                PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReservationProduitLignes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReservationProduitLignes_Reservations_ReservationId",
                    column: x => x.ReservationId,
                    principalTable: "Reservations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReservationServiceLignes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ReservationId = table.Column<int>(type: "INTEGER", nullable: false),
                ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                Designation = table.Column<string>(type: "TEXT", nullable: false),
                Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReservationServiceLignes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReservationServiceLignes_Reservations_ReservationId",
                    column: x => x.ReservationId,
                    principalTable: "Reservations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ReservationServiceLignes_Services_ServiceId",
                    column: x => x.ServiceId,
                    principalTable: "Services",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Product lines only — uses columns that always existed on LocationLignes.
        // Service rows are staged by App.StageLocationServiceLinesBeforeMigrate into __mig_location_services.
        migrationBuilder.Sql("""
            INSERT INTO "ReservationProduitLignes"
                ("Id", "ReservationId", "ProduitId", "Designation", "Quantite", "QuantiteRetournee",
                 "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId")
            SELECT "Id", "LocationId", "ProduitId", "Designation", "Quantite", "QuantiteRetournee",
                   "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId"
            FROM "LocationLignes";
            """);

        migrationBuilder.Sql("""
            INSERT INTO "ReservationServiceLignes"
                ("ReservationId", "ServiceId", "Designation", "Quantite",
                 "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId")
            SELECT "LocationId", "ServiceId", "Designation", "Quantite",
                   "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId"
            FROM "__mig_location_services";
            """);

        migrationBuilder.Sql("DROP TABLE IF EXISTS \"__mig_location_services\";");
        migrationBuilder.DropTable(name: "LocationLignes");

        migrationBuilder.CreateIndex(
            name: "IX_ReservationProduitLignes_ProduitId",
            table: "ReservationProduitLignes",
            column: "ProduitId");

        migrationBuilder.CreateIndex(
            name: "IX_ReservationProduitLignes_ReservationId",
            table: "ReservationProduitLignes",
            column: "ReservationId");

        migrationBuilder.CreateIndex(
            name: "IX_ReservationServiceLignes_ReservationId",
            table: "ReservationServiceLignes",
            column: "ReservationId");

        migrationBuilder.CreateIndex(
            name: "IX_ReservationServiceLignes_ServiceId",
            table: "ReservationServiceLignes",
            column: "ServiceId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LocationLignes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                Designation = table.Column<string>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", nullable: false),
                PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                QuantiteRetournee = table.Column<decimal>(type: "TEXT", nullable: false),
                Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocationLignes", x => x.Id);
            });

        migrationBuilder.Sql("""
            INSERT INTO "LocationLignes"
                ("Id", "LocationId", "ProduitId", "ServiceId", "Designation", "Quantite", "QuantiteRetournee",
                 "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId")
            SELECT "Id", "ReservationId", "ProduitId", NULL, "Designation", "Quantite", "QuantiteRetournee",
                   "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId"
            FROM "ReservationProduitLignes";

            INSERT INTO "LocationLignes"
                ("LocationId", "ProduitId", "ServiceId", "Designation", "Quantite", "QuantiteRetournee",
                 "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId")
            SELECT "ReservationId", NULL, "ServiceId", "Designation", "Quantite", 0,
                   "PrixUnitaireHT", "Remise", "TauxTVA", "Note", "CreatedAt", "UpdatedAt", "CreatedByUserId"
            FROM "ReservationServiceLignes";
            """);

        migrationBuilder.DropTable(name: "ReservationProduitLignes");
        migrationBuilder.DropTable(name: "ReservationServiceLignes");

        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_BonLivraisonId;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_ClientId;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_FactureId;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_Numero;");

        migrationBuilder.Sql("ALTER TABLE Reservations RENAME TO Locations;");

        migrationBuilder.CreateIndex(name: "IX_Locations_BonLivraisonId", table: "Locations", column: "BonLivraisonId");
        migrationBuilder.CreateIndex(name: "IX_Locations_ClientId", table: "Locations", column: "ClientId");
        migrationBuilder.CreateIndex(name: "IX_Locations_FactureId", table: "Locations", column: "FactureId");
        migrationBuilder.CreateIndex(name: "IX_Locations_Numero", table: "Locations", column: "Numero");

        migrationBuilder.CreateIndex(name: "IX_LocationLignes_LocationId", table: "LocationLignes", column: "LocationId");
        migrationBuilder.CreateIndex(name: "IX_LocationLignes_ProduitId", table: "LocationLignes", column: "ProduitId");
        migrationBuilder.CreateIndex(name: "IX_LocationLignes_ServiceId", table: "LocationLignes", column: "ServiceId");
    }
}
