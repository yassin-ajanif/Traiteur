using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reservation.Services;

public sealed class ReservationWorkflowService : IReservationWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;

    public ReservationWorkflowService(IDbContextFactory<AppDbContext> dbFactory, IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _stock = stock;
    }

    public async Task ResyncStockAsync(int reservationId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var res = await db.Reservations
            .Include(l => l.ProduitLignes)
            .FirstAsync(l => l.Id == reservationId, cancellationToken);
        await ApplyStockAsync(db, res, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }

    public Task ClearStockAsync(AppDbContext db, int reservationId, string numero, int? userId, CancellationToken cancellationToken = default) =>
        _stock.ResyncLocationStockAsync(db, reservationId, numero, [], userId, cancellationToken);

    private Task ApplyStockAsync(AppDbContext db, Models.Reservation res, int? userId, CancellationToken cancellationToken)
    {
        var lines = res.ProduitLignes
            .Where(l => l.ProduitId is > 0)
            .Select(l =>
            {
                var encore = l.Quantite - l.QuantiteRetournee;
                return (l.ProduitId!.Value, encore > 0 ? encore : 0m);
            });

        return _stock.ResyncLocationStockAsync(db, res.Id, res.Numero, lines, userId, cancellationToken);
    }
}
