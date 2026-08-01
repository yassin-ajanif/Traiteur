using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reservation.Services;

public sealed class ReservationAvailabilityService : IReservationAvailabilityService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReservationAvailabilityService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<ReservationAvailabilityConflict>> CheckAsync(
        int? excludeReservationId,
        DateTime dateDebut,
        DateTime dateFin,
        IEnumerable<ReservationAvailabilityLineRequest> lines,
        CancellationToken cancellationToken = default)
    {
        var periodStart = dateDebut.Date;
        var periodEnd = dateFin.Date;
        if (periodEnd < periodStart)
            (periodStart, periodEnd) = (periodEnd, periodStart);

        var requested = lines
            .Where(l => l.ProduitId > 0)
            .GroupBy(l => l.ProduitId)
            .Select(g => (
                ProduitId: g.Key,
                Demande: g.Sum(x => Math.Max(0m, x.Quantite - x.QuantiteRetournee))))
            .Where(x => x.Demande > 0)
            .ToList();

        if (requested.Count == 0)
            return [];

        var produitIds = requested.Select(x => x.ProduitId).ToList();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var produits = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Reference, p.Designation, p.StockActuel })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var allOpenLines = await db.ReservationProduitLignes.AsNoTracking()
            .Where(l => l.ProduitId != null && produitIds.Contains(l.ProduitId.Value))
            .Where(l => l.Quantite > l.QuantiteRetournee)
            .Select(l => new
            {
                ReservationId = l.ReservationId,
                ProduitId = l.ProduitId!.Value,
                Encore = l.Quantite - l.QuantiteRetournee
            })
            .ToListAsync(cancellationToken);

        var ownedByProduit = new Dictionary<int, decimal>();
        foreach (var pid in produitIds)
        {
            var stock = produits.TryGetValue(pid, out var p) ? p.StockActuel : 0m;
            var outQty = allOpenLines.Where(l => l.ProduitId == pid).Sum(l => l.Encore);
            ownedByProduit[pid] = stock + outQty;
        }

        var overlapping = await db.Reservations.AsNoTracking()
            .Where(r => excludeReservationId == null || r.Id != excludeReservationId.Value)
            .Select(r => new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                r.DateDebut,
                DateFin = r.DateRetourEffective ?? r.DateFinPrevue
            })
            .ToListAsync(cancellationToken);

        overlapping = overlapping
            .Where(r => PeriodsOverlap(periodStart, periodEnd, r.DateDebut.Date, r.DateFin.Date))
            .ToList();

        var overlapIds = overlapping.Select(r => r.Id).ToHashSet();
        var clientIds = overlapping.Select(r => r.ClientId).Distinct().ToList();
        var clientNames = clientIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Tiers.AsNoTracking()
                .Where(t => clientIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, cancellationToken);

        var overlapById = overlapping.ToDictionary(r => r.Id);
        var otherOpenOnOverlap = allOpenLines
            .Where(l => overlapIds.Contains(l.ReservationId))
            .ToList();

        var conflicts = new List<ReservationAvailabilityConflict>();
        foreach (var req in requested)
        {
            var deja = otherOpenOnOverlap.Where(l => l.ProduitId == req.ProduitId).Sum(l => l.Encore);
            var owned = ownedByProduit.GetValueOrDefault(req.ProduitId);
            var disponible = owned - deja;
            if (req.Demande <= disponible)
                continue;

            produits.TryGetValue(req.ProduitId, out var prod);
            var sources = otherOpenOnOverlap
                .Where(l => l.ProduitId == req.ProduitId)
                .GroupBy(l => l.ReservationId)
                .Select(g =>
                {
                    var res = overlapById[g.Key];
                    clientNames.TryGetValue(res.ClientId, out var nom);
                    return new ReservationAvailabilityConflictSource(
                        res.Numero,
                        string.IsNullOrWhiteSpace(nom) ? $"#{res.ClientId}" : nom,
                        res.DateDebut.Date,
                        res.DateFin.Date,
                        g.Sum(x => x.Encore));
                })
                .OrderBy(s => s.DateDebut)
                .ThenBy(s => s.Numero)
                .ToList();

            conflicts.Add(new ReservationAvailabilityConflict(
                req.ProduitId,
                prod?.Reference ?? string.Empty,
                prod?.Designation ?? string.Empty,
                req.Demande,
                Math.Max(0, disponible),
                owned,
                deja,
                sources));
        }

        return conflicts;
    }

    private static bool PeriodsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
        aStart <= bEnd && bStart <= aEnd;
}
