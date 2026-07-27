using GestionCommerciale.Modules.Location.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Location.Services;

public sealed class LocationWorkflowService : ILocationWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;

    public LocationWorkflowService(IDbContextFactory<AppDbContext> dbFactory, IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _stock = stock;
    }

    public async Task ResyncStockAsync(int locationId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var loc = await db.Locations.Include(l => l.Lignes).FirstAsync(l => l.Id == locationId, cancellationToken);
        await ApplyStockAsync(db, loc, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }

    public Task ClearStockAsync(AppDbContext db, int locationId, string numero, int? userId, CancellationToken cancellationToken = default) =>
        _stock.ResyncLocationStockAsync(db, locationId, numero, [], userId, cancellationToken);

    private Task ApplyStockAsync(AppDbContext db, Models.Location loc, int? userId, CancellationToken cancellationToken)
    {
        if (loc.Statut is StatutLocation.Brouillon or StatutLocation.Annulee)
            return _stock.ResyncLocationStockAsync(db, loc.Id, loc.Numero, [], userId, cancellationToken);

        var lines = loc.Lignes
            .Where(l => l.ProduitId is > 0)
            .Select(l =>
            {
                var encore = l.Quantite - l.QuantiteRetournee;
                return (l.ProduitId!.Value, encore > 0 ? encore : 0m);
            });

        return _stock.ResyncLocationStockAsync(db, loc.Id, loc.Numero, lines, userId, cancellationToken);
    }
}
