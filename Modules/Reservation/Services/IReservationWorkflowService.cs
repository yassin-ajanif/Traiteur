using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Reservation.Services;

public interface IReservationWorkflowService
{
    Task ResyncStockAsync(int reservationId, int? userId, CancellationToken cancellationToken = default);

    Task ClearStockAsync(AppDbContext db, int reservationId, string numero, int? userId, CancellationToken cancellationToken = default);
}
