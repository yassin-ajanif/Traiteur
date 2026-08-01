namespace GestionCommerciale.Modules.Reservation.Services;

public sealed record ReservationAvailabilityConflict(
    int ProduitId,
    string Reference,
    string Designation,
    decimal Demande,
    decimal Disponible,
    decimal StockTotal,
    decimal DejaReserve,
    IReadOnlyList<ReservationAvailabilityConflictSource> Sources);

public sealed record ReservationAvailabilityConflictSource(
    string Numero,
    string ClientNom,
    DateTime DateDebut,
    DateTime DateFin,
    decimal QuantiteEncore);

public sealed record ReservationAvailabilityLineRequest(
    int ProduitId,
    decimal Quantite,
    decimal QuantiteRetournee);

public interface IReservationAvailabilityService
{
    /// <summary>
    /// Soft check: products whose requested qty exceeds capacity on the overlapping period.
    /// </summary>
    Task<IReadOnlyList<ReservationAvailabilityConflict>> CheckAsync(
        int? excludeReservationId,
        DateTime dateDebut,
        DateTime dateFin,
        IEnumerable<ReservationAvailabilityLineRequest> lines,
        CancellationToken cancellationToken = default);
}
