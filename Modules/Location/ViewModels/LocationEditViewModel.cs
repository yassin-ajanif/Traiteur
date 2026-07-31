using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Modules.Livraison.ViewModels;
using GestionCommerciale.Modules.Location.Models;
using GestionCommerciale.Modules.Location.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiersEntity = GestionCommerciale.Modules.Tiers.Models.Tiers;
using TypeTiers = GestionCommerciale.Modules.Tiers.Models.TypeTiers;

namespace GestionCommerciale.Modules.Location.ViewModels;

public partial class LocationEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly ILocationWorkflowService _workflow;
    private readonly AddLineCatalogSearchCoordinator _addLineSearch;

    public LocationEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IAppSettingsService settings,
        ICatalogSearchService catalogSearch,
        ILocationWorkflowService workflow)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _settings = settings;
        _workflow = workflow;
        _addLineSearch = new AddLineCatalogSearchCoordinator(catalogSearch);
        _locale.CultureApplied += (_, _) => RefreshUi();
        Lignes.CollectionChanged += LignesOnCollectionChanged;
        Title = _locale.T("Loc_Title");
        RefreshUi();
    }

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnToBl = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    [ObservableProperty] private string _lblClient = string.Empty;
    [ObservableProperty] private string _wmClientSearch = string.Empty;
    [ObservableProperty] private string _lblDate = string.Empty;
    [ObservableProperty] private string _lblDateDebut = string.Empty;
    [ObservableProperty] private string _lblDateFin = string.Empty;
    [ObservableProperty] private string _lblDateRetour = string.Empty;
    [ObservableProperty] private string _lblStatut = string.Empty;
    [ObservableProperty] private string _lblCaution = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblAddProduct = string.Empty;
    [ObservableProperty] private string _wmAddProduct = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;
    [ObservableProperty] private string _lblDocColRef = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColQteRetour = string.Empty;
    [ObservableProperty] private string _lblDocColPuHt = string.Empty;
    [ObservableProperty] private string _lblDocColRemise = string.Empty;
    [ObservableProperty] private string _lblDocColTva = string.Empty;
    [ObservableProperty] private string _lblDocColMontantHt = string.Empty;
    [ObservableProperty] private string _lblDocColMontantTtc = string.Empty;

    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;
    public AutoCompleteFilterPredicate<object?> CatalogAutocompleteFilter => DocumentCatalogAutoComplete.ItemFilter;

    public ObservableCollection<DocumentCatalogItem> AddLineSearchResults => _addLineSearch.Results;

    [ObservableProperty] private decimal _totalHt;
    [ObservableProperty] private decimal _totalTva;
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private string _totalHtLabel = "HT 0,00";
    [ObservableProperty] private string _totalTvaLabel = "TVA 0,00";
    [ObservableProperty] private string _totalTtcLabel = "TTC 0,00";
    [ObservableProperty] private string _devise = string.Empty;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;
    private bool _suppressAddLinePick;

    private void LignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (LocationLineRow row in e.NewItems)
                row.PropertyChanged += LineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (LocationLineRow row in e.OldItems)
                row.PropertyChanged -= LineOnPropertyChanged;
        RefreshTotals();
        RefreshDerivedStatut();
    }

    private void LineOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LocationLineRow.ProduitId)
            && sender is LocationLineRow row && row.ProduitId is > 0)
            ConsolidateDuplicateProductLines();
        if (e.PropertyName is nameof(LocationLineRow.ServiceId)
            && sender is LocationLineRow serviceRow && serviceRow.ServiceId is > 0)
            ConsolidateDuplicateServiceLines();
        if (e.PropertyName is nameof(LocationLineRow.Quantite) or nameof(LocationLineRow.QuantiteRetournee))
            RefreshDerivedStatut();
        RefreshTotals();
    }

    partial void OnAddLineSearchTextChanged(string value)
    {
        if (_suppressAddLinePick) return;
        _addLineSearch.QueueSearch(value);
    }

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnToBl = _locale.T("Btn_ToBL");
        MenuDelete = _locale.T("Loc_MenuDelete");
        LblClient = _locale.T("Lbl_Client");
        WmClientSearch = _locale.T("Wm_SearchClient");
        LblDate = _locale.T("Loc_LblDate");
        LblDateDebut = _locale.T("Loc_LblDateDebut");
        LblDateFin = _locale.T("Loc_LblDateFin");
        LblDateRetour = _locale.T("Loc_LblDateRetour");
        LblStatut = _locale.T("Loc_ColStatut");
        LblCaution = _locale.T("Loc_LblCaution");
        LblNote = _locale.T("DevisList_ColNote");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblAddProduct = _locale.T("Devis_LblAddProduct");
        WmAddProduct = _locale.T("Wm_SearchCatalog");
        LblTotals = _locale.T("Lbl_Totals");
        LblDocColRef = _locale.T("DocLine_ColRef");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("Loc_ColQteLouee");
        LblDocColQteRetour = _locale.T("Loc_ColQteRetour");
        LblDocColPuHt = _locale.T("DocLine_ColPuHt");
        LblDocColRemise = _locale.T("DocLine_ColRemise");
        LblDocColTva = _locale.T("DocLine_ColTva");
        LblDocColMontantHt = _locale.T("DocLine_ColMontantHt");
        LblDocColMontantTtc = _locale.T("DocLine_ColMontantTtc");
        NotifyStatutChip();
        UpdateTotalLabels(TotalHt, TotalTva, TotalTtc);
    }

    public ObservableCollection<TiersEntity> Clients { get; } = [];
    public ObservableCollection<LocationLineRow> Lignes { get; } = [];

    [ObservableProperty] private int? _locationId;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private TiersEntity? _selectedClient;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private DateTime _dateDebut = DateTime.Today;
    [ObservableProperty] private DateTime _dateFinPrevue = DateTime.Today.AddDays(1);
    [ObservableProperty] private DateTime? _dateRetourEffective;
    [ObservableProperty] private StatutLocation _statut = StatutLocation.EnCours;
    [ObservableProperty] private string _statutLabel = string.Empty;
    [ObservableProperty] private IBrush _statutChipBackground = Brushes.Transparent;
    [ObservableProperty] private IBrush _statutChipForeground = Brushes.Black;
    [ObservableProperty] private IBrush _statutChipBorder = Brushes.Transparent;
    [ObservableProperty] private decimal _caution;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private LocationLineRow? _selectedLine;
    [ObservableProperty] private int? _bonLivraisonId;
    [ObservableProperty] private string _blLabel = string.Empty;

    public bool HasBlLabel => !string.IsNullOrEmpty(BlLabel);

    partial void OnLocationIdChanged(int? value) => RemoveLocationCommand.NotifyCanExecuteChanged();

    partial void OnStatutChanged(StatutLocation value) => NotifyStatutChip();

    partial void OnBlLabelChanged(string value) => OnPropertyChanged(nameof(HasBlLabel));

    private void NotifyStatutChip()
    {
        StatutLabel = LocationStatutLabels.Format(_locale, Statut);
        StatutChipBackground = LocationStatutLabels.ChipBackground(Statut);
        StatutChipForeground = LocationStatutLabels.ChipForeground(Statut);
        StatutChipBorder = LocationStatutLabels.ChipBorder(Statut);
    }

    private void ClearBlLinkUi()
    {
        BonLivraisonId = null;
        BlLabel = string.Empty;
    }

    private async Task RefreshBlLabelAsync(AppDbContext db, int? blId, CancellationToken cancellationToken)
    {
        BonLivraisonId = blId;
        if (blId is not { } id)
        {
            BlLabel = string.Empty;
            return;
        }

        var num = await db.BonsLivraison.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => b.Numero)
            .FirstOrDefaultAsync(cancellationToken);
        BlLabel = string.IsNullOrEmpty(num) ? string.Empty : _locale.Tf("Loc_BlChip", num);
    }

    private bool CanRemoveLocation() => LocationId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveLocation))]
    private async Task RemoveLocationAsync(CancellationToken cancellationToken)
    {
        if (LocationId is not { } id) return;

        if (!await _dialog.ConfirmAsync(_locale.T("Loc_Title"), _locale.Tf("Loc_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);
            await _workflow.ClearStockAsync(db, id, Numero, _session.UserId, cancellationToken);
            var entity = await db.Locations.Include(b => b.Lignes).FirstAsync(b => b.Id == id, cancellationToken);
            db.Locations.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await trx.CommitAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("Loc_Title"), _locale.T("Loc_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression de la location", ex, "LocationEditViewModel.RemoveLocationAsync");
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick) return;
        if (value is not DocumentCatalogItem item) return;
        _suppressAddLinePick = true;
        const decimal addQty = 1;
        var existing = item.Kind == DocumentCatalogKind.Service
            ? Lignes.FirstOrDefault(l => l.ServiceId == item.Id && item.Id != 0)
            : Lignes.FirstOrDefault(l => l.ProduitId == item.Id && item.Id != 0);
        if (existing != null)
        {
            existing.Quantite += addQty;
            SelectedLine = existing;
        }
        else
        {
            var row = new LocationLineRow();
            row.ApplyCatalogItem(item);
            row.Quantite = addQty;
            Lignes.Add(row);
            SelectedLine = row;
        }

        _addLineSearch.ResetAfterPick(
            () =>
            {
                AddLineCatalogPick = null;
                AddLineSearchText = string.Empty;
            },
            () => _suppressAddLinePick = false);
        RefreshTotals();
    }

    private void ConsolidateDuplicateProductLines()
    {
        foreach (var g in Lignes.Where(l => l.ProduitId is > 0).GroupBy(l => l.ProduitId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => Lignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedLine, line))
                    SelectedLine = keep;
                line.PropertyChanged -= LineOnPropertyChanged;
                Lignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void ConsolidateDuplicateServiceLines()
    {
        foreach (var g in Lignes.Where(l => l.ServiceId is > 0).GroupBy(l => l.ServiceId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => Lignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedLine, line))
                    SelectedLine = keep;
                line.PropertyChanged -= LineOnPropertyChanged;
                Lignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void RefreshTotals()
    {
        var ht = Lignes.Sum(l => l.MontantHt);
        var tva = Lignes.Sum(l => l.MontantHt * (l.TauxTva / 100m));
        var ttc = ht + tva;
        TotalHt = ht;
        TotalTva = tva;
        TotalTtc = ttc;
        UpdateTotalLabels(ht, tva, ttc);
    }

    private void UpdateTotalLabels(decimal ht, decimal tva, decimal ttc)
    {
        TotalHtLabel = _locale.Tf("Doc_FmtHt", ht, Devise).TrimEnd();
        TotalTvaLabel = _locale.Tf("Doc_FmtTva", tva, Devise).TrimEnd();
        TotalTtcLabel = _locale.Tf("Doc_FmtTtc", ttc, Devise).TrimEnd();
    }

    private void RefreshDerivedStatut()
    {
        var next = LocationStatutLabels.FromQuantites(
            Lignes.Select(l => (l.Quantite, l.QuantiteRetournee)));

        if (Statut != next)
            Statut = next;
        else
            NotifyStatutChip();

        if (next == StatutLocation.Retournee && DateRetourEffective == null)
            DateRetourEffective = DateTime.Today;
        else if (next != StatutLocation.Retournee)
            DateRetourEffective = null;
    }

    partial void OnDeviseChanged(string value) => RefreshTotals();

    partial void OnSelectedClientChanged(TiersEntity? value)
    {
        var id = value?.Id ?? 0;
        if (ClientId == id) return;
        ClientId = id;
    }

    partial void OnClientIdChanged(int value)
    {
        if (SelectedClient?.Id == value) return;
        SelectedClient = Clients.FirstOrDefault(c => c.Id == value);
    }

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        LocationId = id;
        Lignes.Clear();
        SelectedLine = null;
        ResetAddProductSearch();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(cancellationToken);
        Clients.Clear();
        foreach (var c in clients) Clients.Add(c);

        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);

        if (id == null)
        {
            Numero = "(brouillon)";
            ClientId = Clients.FirstOrDefault()?.Id ?? 0;
            Date = DateTime.Today;
            DateDebut = DateTime.Today;
            DateFinPrevue = DateTime.Today.AddDays(1);
            DateRetourEffective = null;
            Statut = StatutLocation.EnCours;
            Caution = 0;
            Note = string.Empty;
            ClearBlLinkUi();
            Title = _locale.T("Loc_NewTitle");
            RefreshTotals();
            RefreshDerivedStatut();
            return;
        }

        var b = await db.Locations.Include(x => x.Lignes).FirstAsync(x => x.Id == id, cancellationToken);
        Numero = b.Numero;
        ClientId = b.ClientId;
        Date = b.Date.Date;
        DateDebut = b.DateDebut.Date;
        DateFinPrevue = b.DateFinPrevue.Date;
        DateRetourEffective = b.DateRetourEffective?.Date;
        Statut = LocationStatutLabels.Normalize(b.Statut);
        Caution = b.Caution;
        Note = b.Note;
        await RefreshBlLabelAsync(db, b.BonLivraisonId, cancellationToken);
        var produitIds = b.Lignes.Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
        var serviceIds = b.Lignes.Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value).Distinct().ToList();
        var refs = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Reference, cancellationToken);
        var serviceRefs = await db.Services.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Reference, cancellationToken);
        foreach (var l in b.Lignes)
        {
            string reference;
            if (l.ServiceId is { } sid && serviceRefs.TryGetValue(sid, out var sr))
                reference = sr;
            else if (l.ProduitId is { } pid && refs.TryGetValue(pid, out var r))
                reference = r;
            else
                reference = string.Empty;

            Lignes.Add(new LocationLineRow
            {
                ProduitId = l.ProduitId,
                ServiceId = l.ServiceId,
                Reference = reference,
                Designation = l.Designation,
                Quantite = l.Quantite,
                QuantiteRetournee = l.QuantiteRetournee,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA,
                Note = l.Note
            });
        }

        Title = _locale.Tf("Loc_TitleNum", Numero);
        RefreshTotals();
        RefreshDerivedStatut();
        ResetAddProductSearch();
    }

    private void ResetAddProductSearch()
    {
        _suppressAddLinePick = true;
        AddLineCatalogPick = null;
        AddLineSearchText = string.Empty;
        _suppressAddLinePick = false;
        _addLineSearch.Clear();
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    [RelayCommand]
    private void RemoveLine(LocationLineRow? row)
    {
        if (row == null) return;
        Lignes.Remove(row);
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedLine == null) return;
        RemoveLine(SelectedLine);
        SelectedLine = null;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (ClientId == 0 || !Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ErrClientLines"), cancellationToken);
            return;
        }

        if (Lignes.Any(l => l.QuantiteRetournee > l.Quantite))
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ErrQteRetour"), cancellationToken);
            return;
        }

        RefreshDerivedStatut();

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Models.Location entity;
            if (LocationId == null)
            {
                var num = await _numbers.NextLocationAsync(cancellationToken);
                entity = new Models.Location
                {
                    Numero = num,
                    ClientId = ClientId,
                    Date = Date.Date,
                    DateDebut = DateDebut.Date,
                    DateFinPrevue = DateFinPrevue.Date,
                    DateRetourEffective = DateRetourEffective?.Date,
                    Statut = Statut,
                    Caution = Caution,
                    Note = Note,
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(ToEntityLine(l));
                }

                db.Locations.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                LocationId = entity.Id;
            }
            else
            {
                entity = await db.Locations.Include(b => b.Lignes).FirstAsync(b => b.Id == LocationId, cancellationToken);
                entity.ClientId = ClientId;
                entity.Date = Date.Date;
                entity.DateDebut = DateDebut.Date;
                entity.DateFinPrevue = DateFinPrevue.Date;
                entity.DateRetourEffective = DateRetourEffective?.Date;
                entity.Statut = Statut;
                entity.Caution = Caution;
                entity.Note = Note;
                db.LocationLignes.RemoveRange(entity.Lignes);
                foreach (var l in Lignes)
                    entity.Lignes.Add(ToEntityLine(l));

                await db.SaveChangesAsync(cancellationToken);
            }

            await _workflow.ResyncStockAsync(entity.Id, _session.UserId, cancellationToken);

            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("Loc_Title"), _locale.T("Loc_Saved"), cancellationToken);
            await LoadAsync(LocationId, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'enregistrement de la location", ex, "LocationEditViewModel.SaveAsync");
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static LocationLigne ToEntityLine(LocationLineRow l) => new()
    {
        ProduitId = l.ProduitId,
        ServiceId = l.ServiceId,
        Designation = l.Designation,
        Quantite = l.Quantite,
        QuantiteRetournee = l.QuantiteRetournee,
        PrixUnitaireHT = l.PrixUnitaireHt,
        Remise = l.Remise,
        TauxTVA = l.TauxTva,
        Note = l.Note
    };

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<LocationListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ToBlAsync(CancellationToken cancellationToken)
    {
        if (LocationId is not { } locId)
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ToBlNeedSave"), cancellationToken);
            return;
        }

        if (ClientId == 0 || !Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ErrClientLines"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var loc = await db.Locations.Include(l => l.Lignes).FirstAsync(l => l.Id == locId, cancellationToken);

            if (loc.BonLivraisonId is { } existingBlId)
            {
                var exists = await db.BonsLivraison.AsNoTracking().AnyAsync(b => b.Id == existingBlId, cancellationToken);
                if (exists)
                {
                    OpenBl(existingBlId);
                    return;
                }

                loc.BonLivraisonId = null;
                await db.SaveChangesAsync(cancellationToken);
            }

            var blNumero = await _numbers.NextBLAsync(cancellationToken);
            var bl = new BonLivraison
            {
                Numero = blNumero,
                ClientId = loc.ClientId,
                Date = DateTime.Today,
                Note = string.IsNullOrWhiteSpace(loc.Note)
                    ? _locale.Tf("Loc_BlNoteFrom", loc.Numero)
                    : loc.Note,
                CreatedByUserId = _session.UserId
            };
            foreach (var l in loc.Lignes.OrderBy(x => x.Id))
            {
                bl.Lignes.Add(new BonLivraisonLigne
                {
                    ProduitId = l.ProduitId,
                    ServiceId = l.ServiceId,
                    Designation = l.Designation,
                    QuantiteCommandee = l.Quantite,
                    QuantiteLivree = l.Quantite,
                    PrixUnitaireHT = l.PrixUnitaireHT,
                    Remise = l.Remise,
                    TauxTVA = l.TauxTVA,
                    CreatedByUserId = _session.UserId
                });
            }

            db.BonsLivraison.Add(bl);
            await db.SaveChangesAsync(cancellationToken);

            loc.BonLivraisonId = bl.Id;
            await db.SaveChangesAsync(cancellationToken);

            // No stock sync on BL — Location owns stock.
            BonLivraisonId = bl.Id;
            BlLabel = _locale.Tf("Loc_BlChip", bl.Numero);
            OpenBl(bl.Id);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec Vers BL depuis location", ex, "LocationEditViewModel.ToBlAsync");
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenBl(int blId)
    {
        var vm = _sp.GetRequiredService<BLEditViewModel>();
        vm.Load(blId);
        _workspace.Open(vm);
    }
}
