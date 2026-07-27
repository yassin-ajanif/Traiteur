namespace GestionCommerciale.Modules.Livraison.Services;

public interface IBonLivraisonWorkflowService
{
    /// <summary>
    /// Finalizes a BL without stock impact. Physical stock is managed by Location.
    /// Clears any legacy BL stock movements for this document.
    /// </summary>
    Task ValiderAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default);

    /// <summary>Ensures this BL has no stock movements (Location owns stock).</summary>
    Task ResyncStockFromLinesAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default);
}
