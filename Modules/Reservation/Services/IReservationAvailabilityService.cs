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

public enum ProductAvailabilityDayLevel
{
    OutsideMonth,
    Free,
    Partial,
    Full
}

public sealed record ProductAvailabilityDay(
    DateTime Date,
    bool IsCurrentMonth,
    decimal Booked,
    decimal Available,
    decimal StockTotal,
    ProductAvailabilityDayLevel Level);

public sealed record ProductAvailabilityBooking(
    string Numero,
    string ClientNom,
    DateTime DateDebut,
    DateTime DateFin,
    decimal QuantiteEncore);

public sealed record ProductAvailabilityFreeWindow(
    DateTime DateDebut,
    DateTime DateFin,
    decimal AvailableMin);

public sealed record ProductAvailabilityMonthResult(
    int ProduitId,
    string Reference,
    string Designation,
    decimal StockTotal,
    DateTime Month,
    IReadOnlyList<ProductAvailabilityDay> Days,
    IReadOnlyList<ProductAvailabilityBooking> UpcomingBookings,
    IReadOnlyList<ProductAvailabilityFreeWindow> FreeWindows);

public interface IReservationAvailabilityService
{
    Task<IReadOnlyList<ReservationAvailabilityConflict>> CheckAsync(
        int? excludeReservationId,
        DateTime dateDebut,
        DateTime dateFin,
        IEnumerable<ReservationAvailabilityLineRequest> lines,
        CancellationToken cancellationToken = default);

    Task<ProductAvailabilityMonthResult?> GetProductMonthAsync(
        int produitId,
        DateTime month,
        decimal qtyNeeded,
        CancellationToken cancellationToken = default);
}
