using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Location.Services;

public interface ILocationWorkflowService
{
    Task ResyncStockAsync(int locationId, int? userId, CancellationToken cancellationToken = default);

    Task ClearStockAsync(AppDbContext db, int locationId, string numero, int? userId, CancellationToken cancellationToken = default);
}
