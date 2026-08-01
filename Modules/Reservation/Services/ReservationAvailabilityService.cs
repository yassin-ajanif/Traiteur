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

    public async Task<ProductAvailabilityMonthResult?> GetProductMonthAsync(
        int produitId,
        DateTime month,
        decimal qtyNeeded,
        CancellationToken cancellationToken = default)
    {
        if (produitId <= 0)
            return null;

        var needed = Math.Max(1m, qtyNeeded);
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Grid includes days from previous/next month to fill weeks (Monday-first).
        var gridStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
        var gridEnd = gridStart.AddDays(41); // 6 weeks

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var produit = await db.Produits.AsNoTracking()
            .Where(p => p.Id == produitId)
            .Select(p => new { p.Id, p.Reference, p.Designation, p.StockActuel })
            .FirstOrDefaultAsync(cancellationToken);
        if (produit is null)
            return null;

        var openLines = await (
            from l in db.ReservationProduitLignes.AsNoTracking()
            join r in db.Reservations.AsNoTracking() on l.ReservationId equals r.Id
            where l.ProduitId == produitId && l.Quantite > l.QuantiteRetournee
            select new
            {
                r.Id,
                r.Numero,
                r.ClientId,
                r.DateDebut,
                DateFin = r.DateRetourEffective ?? r.DateFinPrevue,
                Encore = l.Quantite - l.QuantiteRetournee
            }).ToListAsync(cancellationToken);

        var owned = produit.StockActuel + openLines.Sum(l => l.Encore);

        // Aggregate bookings that touch the visible grid.
        var relevant = openLines
            .Where(l => PeriodsOverlap(gridStart, gridEnd, l.DateDebut.Date, l.DateFin.Date))
            .ToList();

        var clientIds = relevant.Select(l => l.ClientId).Distinct().ToList();
        var clientNames = clientIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Tiers.AsNoTracking()
                .Where(t => clientIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, cancellationToken);

        var bookedByDay = new Dictionary<DateTime, decimal>();
        foreach (var b in relevant)
        {
            var start = b.DateDebut.Date;
            var end = b.DateFin.Date;
            if (end < start) (start, end) = (end, start);
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (d < gridStart || d > gridEnd) continue;
                bookedByDay.TryGetValue(d, out var sum);
                bookedByDay[d] = sum + b.Encore;
            }
        }

        var days = new List<ProductAvailabilityDay>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var inMonth = date.Month == monthStart.Month;
            bookedByDay.TryGetValue(date, out var booked);
            var available = Math.Max(0, owned - booked);
            ProductAvailabilityDayLevel level;
            if (!inMonth)
                level = ProductAvailabilityDayLevel.OutsideMonth;
            else if (available >= needed && booked <= 0)
                level = ProductAvailabilityDayLevel.Free;
            else if (available >= needed)
                level = ProductAvailabilityDayLevel.Partial;
            else
                level = ProductAvailabilityDayLevel.Full;

            days.Add(new ProductAvailabilityDay(date, inMonth, booked, available, owned, level));
        }

        var today = DateTime.Today;
        var upcomingBookings = relevant
            .Where(l => l.DateFin.Date >= today)
            .GroupBy(l => l.Id)
            .Select(g =>
            {
                var first = g.First();
                clientNames.TryGetValue(first.ClientId, out var nom);
                return new ProductAvailabilityBooking(
                    first.Numero,
                    string.IsNullOrWhiteSpace(nom) ? $"#{first.ClientId}" : nom,
                    first.DateDebut.Date,
                    first.DateFin.Date,
                    g.Sum(x => x.Encore));
            })
            .OrderBy(b => b.DateDebut)
            .ThenBy(b => b.Numero)
            .ToList();

        var freeWindows = BuildFreeWindows(
            days.Where(d => d.IsCurrentMonth && d.Date >= today).OrderBy(d => d.Date),
            needed);

        return new ProductAvailabilityMonthResult(
            produit.Id,
            produit.Reference,
            produit.Designation,
            owned,
            monthStart,
            days,
            upcomingBookings,
            freeWindows);
    }

    private static List<ProductAvailabilityFreeWindow> BuildFreeWindows(
        IEnumerable<ProductAvailabilityDay> futureDays,
        decimal needed)
    {
        var windows = new List<ProductAvailabilityFreeWindow>();
        DateTime? start = null;
        DateTime? end = null;
        decimal minAvail = 0;

        foreach (var day in futureDays)
        {
            var ok = day.Available >= needed;
            if (ok)
            {
                if (start is null)
                {
                    start = day.Date;
                    end = day.Date;
                    minAvail = day.Available;
                }
                else
                {
                    end = day.Date;
                    minAvail = Math.Min(minAvail, day.Available);
                }
            }
            else if (start is not null && end is not null)
            {
                windows.Add(new ProductAvailabilityFreeWindow(start.Value, end.Value, minAvail));
                start = end = null;
            }
        }

        if (start is not null && end is not null)
            windows.Add(new ProductAvailabilityFreeWindow(start.Value, end.Value, minAvail));

        return windows;
    }

    private static bool PeriodsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
        aStart <= bEnd && bStart <= aEnd;
}
