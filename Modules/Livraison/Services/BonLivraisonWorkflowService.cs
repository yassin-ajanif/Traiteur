using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Livraison.Services;

public sealed class BonLivraisonWorkflowService : IBonLivraisonWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;

    public BonLivraisonWorkflowService(IDbContextFactory<AppDbContext> dbFactory, IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _stock = stock;
    }

    public async Task ValiderAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default)
    {
        // Stock for physical exits is owned by Location (rental), not BL.
        // Clear any legacy BL stock movements so saving a BL never deducts qty.
        await ClearBlStockAsync(bonLivraisonId, userId, cancellationToken);
    }

    public async Task ResyncStockFromLinesAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default)
    {
        await ClearBlStockAsync(bonLivraisonId, userId, cancellationToken);
    }

    private async Task ClearBlStockAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var bl = await db.BonsLivraison.AsNoTracking().FirstAsync(b => b.Id == bonLivraisonId, cancellationToken);

        await _stock.ResyncBonLivraisonStockAsync(
            db,
            bonLivraisonId,
            bl.Numero,
            Enumerable.Empty<(int ProduitId, decimal QuantiteLivree)>(),
            userId,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }
}
