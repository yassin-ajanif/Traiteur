using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Location.Models;
using GestionCommerciale.Modules.Location.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Location.ViewModels;

public partial class LocationListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly ILocationWorkflowService _workflow;

    public LocationListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        IDialogService dialog,
        ILocaleService locale,
        IAppSettingsService settings,
        ILocationWorkflowService workflow)
    {
        _dbFactory = dbFactory;
        _workspace = workspaceNavigator;
        _sp = sp;
        _dialog = dialog;
        _locale = locale;
        _settings = settings;
        _workflow = workflow;
        _locale.CultureApplied += (_, _) => RefreshListToolbar();
        RefreshListToolbar();
        Title = _locale.T("LocList_Title");
        Pagination = new PaginationHelper(() => _ = LoadPageAsync(CancellationToken.None));
    }

    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    [ObservableProperty] private string _colHeaderRef = string.Empty;
    [ObservableProperty] private string _colHeaderParty = string.Empty;
    [ObservableProperty] private string _colHeaderDate = string.Empty;
    [ObservableProperty] private string _colHeaderPeriode = string.Empty;
    [ObservableProperty] private string _colHeaderStatut = string.Empty;
    [ObservableProperty] private string _colHeaderTtc = string.Empty;
    [ObservableProperty] private string _colHeaderNote = string.Empty;
    [ObservableProperty] private string _searchWatermark = string.Empty;

    public PaginationHelper Pagination { get; }

    private void RefreshListToolbar()
    {
        BtnNew = _locale.T("Btn_New");
        UpdateBtnFilterDateText();
        MenuDelete = _locale.T("Loc_MenuDelete");
        ColHeaderRef = _locale.T("DevisList_ColRef");
        ColHeaderParty = _locale.T("Lbl_Client");
        ColHeaderDate = _locale.T("DevisList_ColDate");
        ColHeaderPeriode = _locale.T("Loc_ColPeriode");
        ColHeaderStatut = _locale.T("Loc_ColStatut");
        ColHeaderTtc = _locale.T("DevisList_ColTtc");
        ColHeaderNote = _locale.T("DevisList_ColNote");
        SearchWatermark = _locale.T("DocList_SearchPlaceholderClient");
        Title = _locale.T("LocList_Title");
    }

    public ObservableCollection<LocationListRow> Items { get; } = [];
    [ObservableProperty] private LocationListRow? _selected;
    [ObservableProperty] private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => _ = LoadPageAsync(CancellationToken.None, true);

    private async Task LoadPageAsync(CancellationToken ct, bool resetPage = false)
    {
        IsBusy = true;
        try
        {
            if (resetPage)
                Pagination.CurrentPage = 1;

            var cfg = await _settings.GetAsync(ct);
            var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise.Trim();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var q = db.Locations.AsNoTracking().Include(b => b.Lignes).AsQueryable();
            if (_dateFrom.HasValue)
                q = q.Where(b => b.Date >= _dateFrom.Value);
            if (_dateTo.HasValue)
                q = q.Where(b => b.Date <= _dateTo.Value);

            var search = SearchText?.Trim();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(loc => EF.Functions.Like(loc.Numero, $"%{search}%")
                    || db.Tiers.AsNoTracking().Any(t => t.Id == loc.ClientId && EF.Functions.Like(t.Nom, $"%{search}%")));

            var total = await q.CountAsync(ct);
            var list = await q.OrderByDescending(b => b.Date)
                .Skip(Pagination.Skip).Take(Pagination.PageSize)
                .ToListAsync(ct);
            var ids = list.Select(b => b.ClientId).Distinct().ToList();
            var noms = await db.Tiers.AsNoTracking()
                .Where(t => ids.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, ct);
            var selId = Selected?.Location.Id;
            Items.Clear();
            foreach (var b in list)
                Items.Add(LocationListRow.Create(b, noms.GetValueOrDefault(b.ClientId) ?? string.Empty, devise, _locale));
            Pagination.TotalCount = total;
            if (selId is { } id)
                Selected = Items.FirstOrDefault(x => x.Location.Id == id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken ct) => LoadPageAsync(ct, true);

    private void UpdateBtnFilterDateText()
    {
        if (_dateFrom.HasValue && _dateTo.HasValue)
            BtnFilterDate = $"{_dateFrom:dd/MM/yy} — {_dateTo:dd/MM/yy}";
        else
            BtnFilterDate = _locale.T("Btn_FilterDate");
    }

    [RelayCommand]
    private async Task FilterDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(_locale.T("Btn_FilterDate"), cancellationToken);
        if (range == null) return;
        if (range.Value.from == DateTime.MinValue && range.Value.to == DateTime.MinValue)
        {
            _dateFrom = null;
            _dateTo = null;
        }
        else
        {
            _dateFrom = range.Value.from;
            _dateTo = range.Value.to;
        }
        UpdateBtnFilterDateText();
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private void NewLocation()
    {
        var vm = _sp.GetRequiredService<LocationEditViewModel>();
        vm.Load(null);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (Selected == null) return;
        var vm = _sp.GetRequiredService<LocationEditViewModel>();
        vm.Load(Selected.Location.Id);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private async Task DeleteLocationAsync(LocationListRow? row, CancellationToken cancellationToken)
    {
        if (row == null) return;
        var item = row.Location;

        if (!await _dialog.ConfirmAsync(_locale.T("Loc_Title"), _locale.Tf("Loc_ConfirmDelete", item.Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);
            await _workflow.ClearStockAsync(db, item.Id, item.Numero, null, cancellationToken);
            var entity = await db.Locations.Include(b => b.Lignes).FirstAsync(b => b.Id == item.Id, cancellationToken);
            db.Locations.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await trx.CommitAsync(cancellationToken);
            if (Selected?.Location.Id == item.Id)
                Selected = null;
            Items.Remove(row);
            await _dialog.ShowInfoAsync(_locale.T("Loc_Title"), _locale.T("Loc_Deleted"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression de la location", ex, "LocationListViewModel.DeleteLocationAsync");
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
