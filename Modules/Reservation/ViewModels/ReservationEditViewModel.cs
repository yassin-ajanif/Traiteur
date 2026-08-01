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
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Modules.Reservation.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiersEntity = GestionCommerciale.Modules.Tiers.Models.Tiers;
using TypeTiers = GestionCommerciale.Modules.Tiers.Models.TypeTiers;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class ReservationEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly IReservationWorkflowService _workflow;
    private readonly AddLineCatalogSearchCoordinator _addLineSearch;

    public ReservationEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IAppSettingsService settings,
        ICatalogSearchService catalogSearch,
        IReservationWorkflowService workflow)
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
        ProduitLignes.CollectionChanged += ProduitLignesOnCollectionChanged;
        ServiceLignes.CollectionChanged += ServiceLignesOnCollectionChanged;
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
    [ObservableProperty] private string _lblProduitsSection = string.Empty;
    [ObservableProperty] private string _lblServicesSection = string.Empty;
    [ObservableProperty] private string _lblDocColRef = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColQteService = string.Empty;
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

    private void ProduitLignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ReservationProduitLineRow row in e.NewItems)
                row.PropertyChanged += ProduitLineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (ReservationProduitLineRow row in e.OldItems)
                row.PropertyChanged -= ProduitLineOnPropertyChanged;
        RefreshTotals();
        RefreshDerivedStatut();
    }

    private void ServiceLignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ReservationServiceLineRow row in e.NewItems)
                row.PropertyChanged += ServiceLineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (ReservationServiceLineRow row in e.OldItems)
                row.PropertyChanged -= ServiceLineOnPropertyChanged;
        RefreshTotals();
    }

    private void ProduitLineOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReservationProduitLineRow.ProduitId)
            && sender is ReservationProduitLineRow row && row.ProduitId is > 0)
            ConsolidateDuplicateProductLines();
        if (e.PropertyName is nameof(ReservationProduitLineRow.Quantite) or nameof(ReservationProduitLineRow.QuantiteRetournee))
            RefreshDerivedStatut();
        RefreshTotals();
    }

    private void ServiceLineOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReservationServiceLineRow.ServiceId)
            && sender is ReservationServiceLineRow row && row.ServiceId is > 0)
            ConsolidateDuplicateServiceLines();
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
        LblProduitsSection = _locale.T("Res_SectionProduits");
        LblServicesSection = _locale.T("Res_SectionServices");
        LblDocColRef = _locale.T("DocLine_ColRef");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("Loc_ColQteLouee");
        LblDocColQteService = _locale.T("Loc_ColQteVendu");
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
    public ObservableCollection<ReservationProduitLineRow> ProduitLignes { get; } = [];
    public ObservableCollection<ReservationServiceLineRow> ServiceLignes { get; } = [];

    [ObservableProperty] private int? _reservationId;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private TiersEntity? _selectedClient;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private DateTime _dateDebut = DateTime.Today;
    [ObservableProperty] private DateTime _dateFinPrevue = DateTime.Today.AddDays(1);
    [ObservableProperty] private DateTime? _dateRetourEffective;
    [ObservableProperty] private StatutReservation _statut = StatutReservation.EnCours;
    [ObservableProperty] private string _statutLabel = string.Empty;
    [ObservableProperty] private IBrush _statutChipBackground = Brushes.Transparent;
    [ObservableProperty] private IBrush _statutChipForeground = Brushes.Black;
    [ObservableProperty] private IBrush _statutChipBorder = Brushes.Transparent;
    [ObservableProperty] private decimal _caution;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private ReservationProduitLineRow? _selectedProduitLine;
    [ObservableProperty] private ReservationServiceLineRow? _selectedServiceLine;
    [ObservableProperty] private int? _bonLivraisonId;
    [ObservableProperty] private string _blLabel = string.Empty;

    public bool HasBlLabel => !string.IsNullOrEmpty(BlLabel);

    partial void OnReservationIdChanged(int? value) => RemoveReservationCommand.NotifyCanExecuteChanged();

    partial void OnStatutChanged(StatutReservation value) => NotifyStatutChip();

    partial void OnBlLabelChanged(string value) => OnPropertyChanged(nameof(HasBlLabel));

    private void NotifyStatutChip()
    {
        StatutLabel = ReservationStatutLabels.Format(_locale, Statut);
        StatutChipBackground = ReservationStatutLabels.ChipBackground(Statut);
        StatutChipForeground = ReservationStatutLabels.ChipForeground(Statut);
        StatutChipBorder = ReservationStatutLabels.ChipBorder(Statut);
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

    private bool CanRemoveReservation() => ReservationId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveReservation))]
    private async Task RemoveReservationAsync(CancellationToken cancellationToken)
    {
        if (ReservationId is not { } id) return;

        if (!await _dialog.ConfirmAsync(_locale.T("Loc_Title"), _locale.Tf("Loc_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);
            await _workflow.ClearStockAsync(db, id, Numero, _session.UserId, cancellationToken);
            var entity = await db.Reservations.Include(b => b.ProduitLignes).Include(b => b.ServiceLignes).FirstAsync(b => b.Id == id, cancellationToken);
            db.Reservations.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await trx.CommitAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("Loc_Title"), _locale.T("Loc_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression de la réservation", ex, "ReservationEditViewModel.RemoveReservationAsync");
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

        if (item.Kind == DocumentCatalogKind.Service)
        {
            var existing = ServiceLignes.FirstOrDefault(l => l.ServiceId == item.Id && item.Id != 0);
            if (existing != null)
            {
                existing.Quantite += addQty;
                SelectedServiceLine = existing;
            }
            else
            {
                var row = new ReservationServiceLineRow();
                row.ApplyCatalogItem(item);
                row.Quantite = addQty;
                ServiceLignes.Add(row);
                SelectedServiceLine = row;
            }
        }
        else
        {
            var existing = ProduitLignes.FirstOrDefault(l => l.ProduitId == item.Id && item.Id != 0);
            if (existing != null)
            {
                existing.Quantite += addQty;
                SelectedProduitLine = existing;
            }
            else
            {
                var row = new ReservationProduitLineRow();
                row.ApplyCatalogItem(item);
                row.Quantite = addQty;
                ProduitLignes.Add(row);
                SelectedProduitLine = row;
            }
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
        foreach (var g in ProduitLignes.Where(l => l.ProduitId is > 0).GroupBy(l => l.ProduitId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => ProduitLignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedProduitLine, line))
                    SelectedProduitLine = keep;
                line.PropertyChanged -= ProduitLineOnPropertyChanged;
                ProduitLignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void ConsolidateDuplicateServiceLines()
    {
        foreach (var g in ServiceLignes.Where(l => l.ServiceId is > 0).GroupBy(l => l.ServiceId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => ServiceLignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedServiceLine, line))
                    SelectedServiceLine = keep;
                line.PropertyChanged -= ServiceLineOnPropertyChanged;
                ServiceLignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void RefreshTotals()
    {
        var ht = ProduitLignes.Sum(l => l.MontantHt) + ServiceLignes.Sum(l => l.MontantHt);
        var tva = ProduitLignes.Sum(l => l.MontantHt * (l.TauxTva / 100m)) + ServiceLignes.Sum(l => l.MontantHt * (l.TauxTva / 100m));
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
        var next = ReservationStatutLabels.FromQuantites(
            ProduitLignes.Select(l => (l.Quantite, l.QuantiteRetournee)));

        if (Statut != next)
            Statut = next;
        else
            NotifyStatutChip();

        if (next == StatutReservation.Retournee && DateRetourEffective == null)
            DateRetourEffective = DateTime.Today;
        else if (next != StatutReservation.Retournee)
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
        ReservationId = id;
        ProduitLignes.Clear();
        ServiceLignes.Clear();
        SelectedProduitLine = null;
        SelectedServiceLine = null;
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
            Statut = StatutReservation.EnCours;
            Caution = 0;
            Note = string.Empty;
            ClearBlLinkUi();
            Title = _locale.T("Loc_NewTitle");
            RefreshTotals();
            RefreshDerivedStatut();
            return;
        }

        var b = await db.Reservations
            .Include(x => x.ProduitLignes)
            .Include(x => x.ServiceLignes)
            .FirstAsync(x => x.Id == id, cancellationToken);
        Numero = b.Numero;
        ClientId = b.ClientId;
        Date = b.Date.Date;
        DateDebut = b.DateDebut.Date;
        DateFinPrevue = b.DateFinPrevue.Date;
        DateRetourEffective = b.DateRetourEffective?.Date;
        Statut = ReservationStatutLabels.Normalize(b.Statut);
        Caution = b.Caution;
        Note = b.Note;
        await RefreshBlLabelAsync(db, b.BonLivraisonId, cancellationToken);

        var produitIds = b.ProduitLignes.Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
        var serviceIds = b.ServiceLignes.Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value).Distinct().ToList();
        var refs = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Reference, cancellationToken);
        var serviceRefs = await db.Services.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Reference, cancellationToken);

        foreach (var l in b.ProduitLignes)
        {
            var reference = l.ProduitId is { } pid && refs.TryGetValue(pid, out var r) ? r : string.Empty;
            ProduitLignes.Add(new ReservationProduitLineRow
            {
                ProduitId = l.ProduitId,
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

        foreach (var l in b.ServiceLignes)
        {
            var reference = l.ServiceId is { } sid && serviceRefs.TryGetValue(sid, out var sr) ? sr : string.Empty;
            ServiceLignes.Add(new ReservationServiceLineRow
            {
                ServiceId = l.ServiceId,
                Reference = reference,
                Designation = l.Designation,
                Quantite = l.Quantite,
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
    private void RemoveProduitLine(ReservationProduitLineRow? row)
    {
        if (row == null) return;
        ProduitLignes.Remove(row);
    }

    [RelayCommand]
    private void RemoveServiceLine(ReservationServiceLineRow? row)
    {
        if (row == null) return;
        ServiceLignes.Remove(row);
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedProduitLine != null)
        {
            var line = SelectedProduitLine;
            SelectedProduitLine = null;
            ProduitLignes.Remove(line);
            return;
        }

        if (SelectedServiceLine != null)
        {
            var line = SelectedServiceLine;
            SelectedServiceLine = null;
            ServiceLignes.Remove(line);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (ClientId == 0 || (!ProduitLignes.Any() && !ServiceLignes.Any()))
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ErrClientLines"), cancellationToken);
            return;
        }

        if (ProduitLignes.Any(l => l.QuantiteRetournee > l.Quantite))
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ErrQteRetour"), cancellationToken);
            return;
        }

        RefreshDerivedStatut();

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Models.Reservation entity;
            if (ReservationId == null)
            {
                var num = await _numbers.NextLocationAsync(cancellationToken);
                entity = new Models.Reservation
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
                foreach (var l in ProduitLignes)
                    entity.ProduitLignes.Add(ToProduitEntityLine(l));
                foreach (var l in ServiceLignes)
                    entity.ServiceLignes.Add(ToServiceEntityLine(l));

                db.Reservations.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                ReservationId = entity.Id;
            }
            else
            {
                entity = await db.Reservations
                    .Include(b => b.ProduitLignes)
                    .Include(b => b.ServiceLignes)
                    .FirstAsync(b => b.Id == ReservationId, cancellationToken);
                entity.ClientId = ClientId;
                entity.Date = Date.Date;
                entity.DateDebut = DateDebut.Date;
                entity.DateFinPrevue = DateFinPrevue.Date;
                entity.DateRetourEffective = DateRetourEffective?.Date;
                entity.Statut = Statut;
                entity.Caution = Caution;
                entity.Note = Note;
                db.ReservationProduitLignes.RemoveRange(entity.ProduitLignes);
                db.ReservationServiceLignes.RemoveRange(entity.ServiceLignes);
                foreach (var l in ProduitLignes)
                    entity.ProduitLignes.Add(ToProduitEntityLine(l));
                foreach (var l in ServiceLignes)
                    entity.ServiceLignes.Add(ToServiceEntityLine(l));

                await db.SaveChangesAsync(cancellationToken);
            }

            await _workflow.ResyncStockAsync(entity.Id, _session.UserId, cancellationToken);

            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("Loc_Title"), _locale.T("Loc_Saved"), cancellationToken);
            await LoadAsync(ReservationId, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'enregistrement de la réservation", ex, "ReservationEditViewModel.SaveAsync");
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static ReservationProduitLigne ToProduitEntityLine(ReservationProduitLineRow l) => new()
    {
        ProduitId = l.ProduitId,
        Designation = l.Designation,
        Quantite = l.Quantite,
        QuantiteRetournee = l.QuantiteRetournee,
        PrixUnitaireHT = l.PrixUnitaireHt,
        Remise = l.Remise,
        TauxTVA = l.TauxTva,
        Note = l.Note
    };

    private static ReservationServiceLigne ToServiceEntityLine(ReservationServiceLineRow l) => new()
    {
        ServiceId = l.ServiceId,
        Designation = l.Designation,
        Quantite = l.Quantite,
        PrixUnitaireHT = l.PrixUnitaireHt,
        Remise = l.Remise,
        TauxTVA = l.TauxTva,
        Note = l.Note
    };

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<ReservationListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ToBlAsync(CancellationToken cancellationToken)
    {
        if (ReservationId is not { } resId)
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ToBlNeedSave"), cancellationToken);
            return;
        }

        if (ClientId == 0 || (!ProduitLignes.Any() && !ServiceLignes.Any()))
        {
            await _dialog.ShowErrorAsync(_locale.T("Loc_Title"), _locale.T("Loc_ErrClientLines"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var res = await db.Reservations
                .Include(l => l.ProduitLignes)
                .Include(l => l.ServiceLignes)
                .FirstAsync(l => l.Id == resId, cancellationToken);

            if (res.BonLivraisonId is { } existingBlId)
            {
                var exists = await db.BonsLivraison.AsNoTracking().AnyAsync(b => b.Id == existingBlId, cancellationToken);
                if (exists)
                {
                    OpenBl(existingBlId);
                    return;
                }

                res.BonLivraisonId = null;
                await db.SaveChangesAsync(cancellationToken);
            }

            var blNumero = await _numbers.NextBLAsync(cancellationToken);
            var bl = new BonLivraison
            {
                Numero = blNumero,
                ClientId = res.ClientId,
                Date = DateTime.Today,
                Note = string.IsNullOrWhiteSpace(res.Note)
                    ? _locale.Tf("Loc_BlNoteFrom", res.Numero)
                    : res.Note,
                CreatedByUserId = _session.UserId
            };
            foreach (var l in res.ProduitLignes.OrderBy(x => x.Id))
            {
                bl.Lignes.Add(new BonLivraisonLigne
                {
                    ProduitId = l.ProduitId,
                    Designation = l.Designation,
                    QuantiteCommandee = l.Quantite,
                    QuantiteLivree = l.Quantite,
                    PrixUnitaireHT = l.PrixUnitaireHT,
                    Remise = l.Remise,
                    TauxTVA = l.TauxTVA,
                    CreatedByUserId = _session.UserId
                });
            }
            foreach (var l in res.ServiceLignes.OrderBy(x => x.Id))
            {
                bl.Lignes.Add(new BonLivraisonLigne
                {
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

            res.BonLivraisonId = bl.Id;
            await db.SaveChangesAsync(cancellationToken);

            // No stock sync on BL — Reservation owns stock.
            BonLivraisonId = bl.Id;
            BlLabel = _locale.Tf("Loc_BlChip", bl.Numero);
            OpenBl(bl.Id);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec Vers BL depuis réservation", ex, "ReservationEditViewModel.ToBlAsync");
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
