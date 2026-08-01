using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class EtatClientViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;

    public EtatClientViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        ILocaleService locale,
        IAppSettingsService settings,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _locale = locale;
        _settings = settings;
        _workspace = workspaceNavigator;
        _sp = sp;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
    }

    [ObservableProperty] private string _searchWatermark = string.Empty;
    [ObservableProperty] private string _chkEnRetardOnly = string.Empty;
    [ObservableProperty] private string _colCaution = string.Empty;
    [ObservableProperty] private string _btnOpenLocation = string.Empty;
    [ObservableProperty] private string _emptyHint = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _enRetardOnly;
    [ObservableProperty] private bool _hasClients;

    public ObservableCollection<EtatClientRow> Clients { get; } = [];

    private List<EtatClientRow> _allClients = [];

    private void RefreshUi()
    {
        Title = _locale.T("Nav_EtatClient");
        SearchWatermark = _locale.T("EtatClient_Search");
        ChkEnRetardOnly = _locale.T("EtatClient_EnRetardOnly");
        ColCaution = _locale.T("Loc_LblCaution");
        BtnOpenLocation = _locale.T("EtatClient_OpenLocation");
        EmptyHint = _locale.T("EtatClient_Empty");
    }

    partial void OnSearchTextChanged(string value) => ApplyClientFilter();
    partial void OnEnRetardOnlyChanged(bool value) => ApplyClientFilter();

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var today = DateTime.Today;
            var cfg = await _settings.GetAsync(cancellationToken);
            var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise.Trim();

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var openLines = await db.ReservationProduitLignes.AsNoTracking()
                .Include(l => l.Reservation)
                .Where(l => l.Reservation != null && l.Quantite > l.QuantiteRetournee)
                .ToListAsync(cancellationToken);

            if (openLines.Count == 0)
            {
                _allClients = [];
                Clients.Clear();
                HasClients = false;
                return;
            }

            var clientIds = openLines.Select(l => l.Reservation!.ClientId).Distinct().ToList();
            var clients = await db.Tiers.AsNoTracking()
                .Where(t => clientIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            var produitIds = openLines.Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
            var refs = await db.Produits.AsNoTracking()
                .Where(p => produitIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Reference, cancellationToken);

            var retardBadge = _locale.T("EtatClient_ColRetard");
            var retardOui = _locale.T("EtatClient_RetardOui");
            var retardNon = _locale.T("EtatClient_RetardNon");
            var encoreSuffix = _locale.T("EtatClient_EncoreSuffix");

            _allClients = openLines
                .GroupBy(l => l.Reservation!.ClientId)
                .Select(g =>
                {
                    clients.TryGetValue(g.Key, out var c);
                    var encore = g.Sum(l => l.Quantite - l.QuantiteRetournee);
                    var fins = g.Select(l => l.Reservation!.DateFinPrevue).ToList();
                    var prochaine = fins.Min();
                    var enRetard = fins.Any(f => f.Date < today);
                    var resIds = g.Select(l => l.ReservationId).Distinct().Count();
                    var caution = g.Select(l => l.Reservation!).GroupBy(x => x.Id).Sum(x => x.First().Caution);
                    var qteLabel = encore.ToString("N0", CultureInfo.CurrentCulture);
                    var finLabel = prochaine.ToString("d", CultureInfo.CurrentCulture);
                    var row = new EtatClientRow
                    {
                        ClientId = g.Key,
                        ClientNom = c?.Nom ?? $"#{g.Key}",
                        ClientTelephone = c?.Telephone ?? string.Empty,
                        NbReservationsOuvertes = resIds,
                        QteEncoreSortie = encore,
                        QteEncoreLabel = qteLabel,
                        ProchaineFinPrevue = prochaine,
                        FinPrevueLabel = finLabel,
                        EstEnRetard = enRetard,
                        RetardBadge = retardBadge,
                        CautionTotale = caution,
                        CautionLabel = $"{caution:N2} {devise}",
                        SummaryLabel = _locale.Tf("EtatClient_CardSummary", qteLabel, resIds, finLabel),
                    };

                    foreach (var item in BuildItems(g, refs, today, retardOui, retardNon, encoreSuffix))
                        row.Items.Add(item);

                    return row;
                })
                .OrderByDescending(r => r.EstEnRetard)
                .ThenBy(r => r.ClientNom)
                .ToList();

            ApplyClientFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static IEnumerable<EtatClientItemRow> BuildItems(
        IGrouping<int, ReservationProduitLigne> lines,
        Dictionary<int, string> refs,
        DateTime today,
        string retardOui,
        string retardNon,
        string encoreSuffix) =>
        lines
            .Select(l =>
            {
                var encore = l.Quantite - l.QuantiteRetournee;
                var enRetard = l.Reservation!.DateFinPrevue.Date < today;
                var pref = l.ProduitId is { } pid && refs.TryGetValue(pid, out var r) ? r : string.Empty;
                var qteLabel = encore.ToString("N2", CultureInfo.CurrentCulture);
                return new EtatClientItemRow
                {
                    ReservationId = l.ReservationId,
                    ReservationNumero = l.Reservation.Numero,
                    ProduitReference = pref,
                    Designation = l.Designation,
                    QuantiteLouee = l.Quantite,
                    QuantiteRetournee = l.QuantiteRetournee,
                    QuantiteEncore = encore,
                    QuantiteEncoreLabel = $"{qteLabel} {encoreSuffix}",
                    DateDebut = l.Reservation.DateDebut,
                    DateFinPrevue = l.Reservation.DateFinPrevue,
                    PeriodeLabel = $"{l.Reservation.DateDebut:dd/MM} → {l.Reservation.DateFinPrevue:dd/MM}",
                    EstEnRetard = enRetard,
                    RetardLabel = enRetard ? retardOui : retardNon,
                    MetaLabel = string.IsNullOrWhiteSpace(pref)
                        ? $"{l.Reservation.Numero}  ·  {l.Reservation.DateDebut:dd/MM} → {l.Reservation.DateFinPrevue:dd/MM}"
                        : $"{pref}  ·  {l.Reservation.Numero}  ·  {l.Reservation.DateDebut:dd/MM} → {l.Reservation.DateFinPrevue:dd/MM}",
                };
            })
            .OrderByDescending(i => i.EstEnRetard)
            .ThenBy(i => i.Designation);

    private void ApplyClientFilter()
    {
        var q = _allClients.AsEnumerable();
        if (EnRetardOnly)
            q = q.Where(c => c.EstEnRetard);

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(c =>
                c.ClientNom.Contains(search, StringComparison.OrdinalIgnoreCase)
                || c.ClientTelephone.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Clients.Clear();
        foreach (var row in q)
            Clients.Add(row);

        HasClients = Clients.Count > 0;
    }

    [RelayCommand]
    private void ToggleExpand(EtatClientRow? row)
    {
        if (row == null) return;
        row.IsExpanded = !row.IsExpanded;
    }

    [RelayCommand]
    private void OpenReservation(EtatClientItemRow? item)
    {
        if (item == null) return;
        var vm = _sp.GetRequiredService<ReservationEditViewModel>();
        vm.Load(item.ReservationId);
        _workspace.Open(vm);
    }
}
