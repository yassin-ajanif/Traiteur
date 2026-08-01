using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Modules.Livraison.Services;
using GestionCommerciale.Modules.Reservation.ViewModels;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Livraison.ViewModels;

using BonCommandeReferenceStorage = GestionCommerciale.Modules.Livraison.BonCommandeReferenceStorage;

public partial class BLEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IBonLivraisonWorkflowService _workflow;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IUiPreferencesService _uiPreferences;
    private readonly IStockMovementService _stock;
    private readonly IPdfService _pdf;
    private readonly IPdfPrintService _pdfPrint;
    private readonly IAppSettingsService _settings;
    private readonly IFactureBlLinkService _blLinkService;
    private readonly AddLineCatalogSearchCoordinator _addLineSearch;

    public BLEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IBonLivraisonWorkflowService workflow,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IUiPreferencesService uiPreferences,
        IStockMovementService stock,
        IPdfService pdf,
        IPdfPrintService pdfPrint,
        IAppSettingsService settings,
        IFactureBlLinkService blLinkService,
        ICatalogSearchService catalogSearch)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _workflow = workflow;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _uiPreferences = uiPreferences;
        _stock = stock;
        _pdf = pdf;
        _pdfPrint = pdfPrint;
        _settings = settings;
        _blLinkService = blLinkService;
        _addLineSearch = new AddLineCatalogSearchCoordinator(catalogSearch);
        _locale.CultureApplied += (_, _) => RefreshBlUi();
        LineGridColumns.PropertyChanged += OnLineGridColumnsPropertyChanged;
        _uiPreferences.LoadDocumentLineColumns("bon_livraison", LineGridColumns);
        Title = _locale.T("BL_Title");
        Lignes.CollectionChanged += LignesOnCollectionChanged;
        RefreshBlUi();
    }

    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _btnPrint = string.Empty;
    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnToInvoice = string.Empty;
    [ObservableProperty] private string _menuDeleteBl = string.Empty;
    [ObservableProperty] private string _lblClient = string.Empty;
    [ObservableProperty] private string _wmClientSearch = string.Empty;
    [ObservableProperty] private string _lblDateBl = string.Empty;
    [ObservableProperty] private string _btnAddLine = string.Empty;
    [ObservableProperty] private string _btnApplyProduct = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblAddProduct = string.Empty;
    [ObservableProperty] private string _wmAddProduct = string.Empty;
    [ObservableProperty] private string _lblDocLineColumnsHint = string.Empty;
    [ObservableProperty] private string _lblDocColRef = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColCond = string.Empty;
    [ObservableProperty] private string _wmDocLineUnite = string.Empty;
    [ObservableProperty] private string _lblDocColPuHt = string.Empty;
    [ObservableProperty] private string _lblDocColRemise = string.Empty;
    [ObservableProperty] private string _lblDocColTva = string.Empty;
    [ObservableProperty] private string _lblDocColMontantHt = string.Empty;
    [ObservableProperty] private string _lblDocColMontantTtc = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;
    [ObservableProperty] private string _invoicedLabel = string.Empty;
    [ObservableProperty] private int? _linkedFactureId;
    [ObservableProperty] private string _bonCommandeReference = string.Empty;
    [ObservableProperty] private int? _reservationId;
    [ObservableProperty] private string _reservationLabel = string.Empty;
    public bool HasInvoicedLabel => !string.IsNullOrEmpty(InvoicedLabel);
    public bool HasReservationLabel => !string.IsNullOrEmpty(ReservationLabel);

    partial void OnInvoicedLabelChanged(string value) => OnPropertyChanged(nameof(HasInvoicedLabel));
    partial void OnReservationLabelChanged(string value) => OnPropertyChanged(nameof(HasReservationLabel));

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

    public DocumentLineGridColumnState LineGridColumns { get; } = new(supportsLineRemise: true);

    public ObservableCollection<DocumentCatalogItem> AddLineSearchResults => _addLineSearch.Results;

    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;

    public bool ShowTotalTva => LineGridColumns.ShowTva && LineGridColumns.ShowMontantTtc;
    public bool ShowTotalTtc => LineGridColumns.ShowMontantTtc && LineGridColumns.ShowTva;
    public bool HighlightHtTotal => !ShowTotalTtc;

    private void OnLineGridColumnsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentLineGridColumnState.ShowTva) or nameof(DocumentLineGridColumnState.ShowMontantTtc))
        {
            OnPropertyChanged(nameof(ShowTotalTva));
            OnPropertyChanged(nameof(ShowTotalTtc));
            OnPropertyChanged(nameof(HighlightHtTotal));
            RefreshTotals();
        }

        _uiPreferences.SaveDocumentLineColumns("bon_livraison", LineGridColumns);
    }

    private void LignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (BLLineRow row in e.NewItems)
                row.PropertyChanged += LineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (BLLineRow row in e.OldItems)
                row.PropertyChanged -= LineOnPropertyChanged;
        RefreshTotals();
    }

    private void LineOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BLLineRow.ProduitId) or nameof(BLLineRow.ServiceId)
            && sender is BLLineRow row && (row.ProduitId is > 0 || row.ServiceId is > 0))
            ConsolidateDuplicateCatalogLines();
        RefreshTotals();
    }

    partial void OnAddLineSearchTextChanged(string value)
    {
        if (_suppressAddLinePick) return;
        _addLineSearch.QueueSearch(value);
    }

    private void RefreshBlUi()
    {
        BtnPdf = _locale.T("Btn_Pdf");
        BtnPrint = _locale.T("Btn_Print");
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnToInvoice = _locale.T("Btn_ToInvoice");
        MenuDeleteBl = _locale.T("BL_MenuDelete");
        LblClient = _locale.T("Lbl_Client");
        WmClientSearch = _locale.T("Wm_SearchClient");
        LblDateBl = _locale.T("Lbl_DateBL");
        BtnAddLine = _locale.T("Btn_AddLine");
        BtnApplyProduct = _locale.T("Btn_ApplyProduct");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblAddProduct = _locale.T("Devis_LblAddProduct");
        WmAddProduct = _locale.T("Wm_SearchCatalog");
        LblDocLineColumnsHint = _locale.T("DocLine_ColumnsHint");
        LblDocColRef = _locale.T("DocLine_ColRef");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("DocLine_ColQte");
        LblDocColCond = _locale.T("DocLine_ColCond");
        WmDocLineUnite = _locale.T("DocLine_WmUnite");
        LblDocColPuHt = _locale.T("DocLine_ColPuHt");
        LblDocColRemise = _locale.T("DocLine_ColRemise");
        LblDocColTva = _locale.T("DocLine_ColTva");
        LblDocColMontantHt = _locale.T("DocLine_ColMontantHt");
        LblDocColMontantTtc = _locale.T("DocLine_ColMontantTtc");
        LblTotals = _locale.T("Lbl_Totals");
        UpdateTotalLabels(TotalHt, TotalTva, TotalTtc);
    }

    public ObservableCollection<GestionCommerciale.Modules.Tiers.Models.Tiers> Clients { get; } = [];
    public ObservableCollection<BLLineRow> Lignes { get; } = [];

    [ObservableProperty] private int? _blId;
    [ObservableProperty] private int? _devisId;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private GestionCommerciale.Modules.Tiers.Models.Tiers? _selectedClient;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private BLLineRow? _selectedLine;

    public bool CanEdit => !IsReadOnly;

    partial void OnBlIdChanged(int? value) => RemoveBlCommand.NotifyCanExecuteChanged();

    private bool CanRemoveBl() => BlId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveBl))]
    private async Task RemoveBlAsync(CancellationToken cancellationToken)
    {
        if (BlId is not { } id) return;

        if (!await _dialog.ConfirmAsync(_locale.T("BL_DlgShort"), _locale.Tf("BL_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var blockedMsg = await BonLivraisonDeleteReferencedMessage.BuildIfBlockedAsync(db, id, _locale, cancellationToken);
            if (blockedMsg != null)
            {
                await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), blockedMsg, cancellationToken);
                return;
            }

            var entity = await db.BonsLivraison.Include(b => b.Lignes).FirstAsync(b => b.Id == id, cancellationToken);
            await _stock.ResyncBonLivraisonStockAsync(db, entity.Id, entity.Numero, Enumerable.Empty<(int ProduitId, decimal QuantiteLivree)>(), null, cancellationToken);
            db.BonsLivraison.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("BL_DlgShort"), _locale.T("BL_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression du bon de livraison", ex, "BLEditViewModel.RemoveBlAsync");
            await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedClientChanged(GestionCommerciale.Modules.Tiers.Models.Tiers? value)
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

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick || !CanEdit) return;
        if (value is not DocumentCatalogItem item) return;
        _suppressAddLinePick = true;
        var existing = item.Kind == DocumentCatalogKind.Product
            ? Lignes.FirstOrDefault(l => l.ProduitId == item.Id && item.Id != 0)
            : Lignes.FirstOrDefault(l => l.ServiceId == item.Id && item.Id != 0);
        if (existing != null)
        {
            existing.QuantiteLivree += 1;
            existing.QuantiteCommandee += 1;
            SelectedLine = existing;
        }
        else
        {
            var row = new BLLineRow();
            row.ApplyCatalogItem(item);
            row.QuantiteCommandee = 1;
            row.QuantiteLivree = 1;
            row.PropertyChanged += LineOnPropertyChanged;
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

    private void ConsolidateDuplicateCatalogLines()
    {
        foreach (var g in Lignes.Where(l => l.ProduitId is > 0).GroupBy(l => l.ProduitId).ToList())
        {
            if (g.Count() < 2) continue;
            MergeDuplicateGroup(g);
        }

        foreach (var g in Lignes.Where(l => l.ServiceId is > 0).GroupBy(l => l.ServiceId).ToList())
        {
            if (g.Count() < 2) continue;
            MergeDuplicateGroup(g);
        }
    }

    private void MergeDuplicateGroup(IEnumerable<BLLineRow> group)
    {
        var ordered = group.OrderBy(l => Lignes.IndexOf(l)).ToList();
        var keep = ordered[0];
        var extraQty = ordered.Skip(1).Sum(l => l.QuantiteLivree);
        foreach (var line in ordered.Skip(1))
        {
            if (ReferenceEquals(SelectedLine, line))
                SelectedLine = keep;
            line.PropertyChanged -= LineOnPropertyChanged;
            Lignes.Remove(line);
        }

        keep.QuantiteLivree += extraQty;
        keep.QuantiteCommandee += extraQty;
    }

    private void ResetAddProductSearch()
    {
        _suppressAddLinePick = true;
        AddLineCatalogPick = null;
        AddLineSearchText = string.Empty;
        _suppressAddLinePick = false;
        _addLineSearch.Clear();
    }

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        BlId = id;
        BonCommandeReference = string.Empty;
        ReservationId = null;
        ReservationLabel = string.Empty;
        DevisId = null;
        Lignes.Clear();
        ResetAddProductSearch();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(cancellationToken);
        Clients.Clear();
        foreach (var c in clients) Clients.Add(c);

        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);

        InvoicedLabel = string.Empty;
        LinkedFactureId = null;

        if (id == null)
        {
            Numero = "(brouillon)";
            ClientId = Clients.FirstOrDefault()?.Id ?? 0;
            IsReadOnly = false;
            Title = _locale.T("BL_NewTitle");
            RefreshTotals();
            return;
        }

        var factNum = await _blLinkService.GetInvoicingStatusAsync(id.Value, cancellationToken);
        if (factNum != null)
        {
            InvoicedLabel = _locale.Tf("BL_FacturedOn", factNum);
            LinkedFactureId = await db.BonsLivraison.AsNoTracking()
                .Where(x => x.Id == id.Value)
                .Select(x => x.FactureId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var b = await db.BonsLivraison.Include(x => x.Lignes).FirstAsync(x => x.Id == id, cancellationToken);
        DevisId = b.DevisId;
        var (storedBccRef, userNote) = BonCommandeReferenceStorage.Parse(b.Note);
        BonCommandeReference = storedBccRef;
        if (string.IsNullOrWhiteSpace(BonCommandeReference) && b.BonCommandeClientId is int linkedBccId)
        {
            BonCommandeReference = await db.BonsCommandeClient.AsNoTracking()
                .Where(x => x.Id == linkedBccId)
                .Select(x => x.Numero)
                .FirstAsync(cancellationToken);
        }

        ReservationId = b.ReservationId;
        if (ReservationId is null)
        {
            ReservationId = await db.Reservations.AsNoTracking()
                .Where(r => r.BonLivraisonId == b.Id)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (ReservationId is { } resId)
        {
            var resNumero = await db.Reservations.AsNoTracking()
                .Where(r => r.Id == resId)
                .Select(r => r.Numero)
                .FirstOrDefaultAsync(cancellationToken);
            ReservationLabel = string.IsNullOrEmpty(resNumero)
                ? string.Empty
                : _locale.Tf("BL_ResChip", resNumero);

            // Strip legacy auto note "Réservation {numero}" now shown as chip.
            if (!string.IsNullOrEmpty(resNumero)
                && string.Equals(userNote.Trim(), _locale.Tf("Loc_BlNoteFrom", resNumero).Trim(), StringComparison.OrdinalIgnoreCase))
            {
                userNote = string.Empty;
            }
        }
        else
        {
            ReservationLabel = string.Empty;
        }

        Numero = b.Numero;
        ClientId = b.ClientId;
        Date = new DateTimeOffset(b.Date);
        Note = userNote;
        var catalogRefs = await DocumentLineCatalogLookups.LoadAsync(
            db,
            b.Lignes.Select(l => (l.ProduitId, l.ServiceId)),
            cancellationToken);
        foreach (var l in b.Lignes)
        {
            var row = new BLLineRow
            {
                ProduitId = l.ProduitId,
                ServiceId = l.ServiceId,
                Reference = catalogRefs.GetReference(l.ProduitId, l.ServiceId),
                Designation = l.Designation,
                QuantiteCommandee = l.QuantiteCommandee,
                QuantiteLivree = l.QuantiteLivree,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA
            };
            row.PropertyChanged += LineOnPropertyChanged;
            Lignes.Add(row);
        }

        IsReadOnly = false;
        Title = _locale.Tf("BL_TitleNum", Numero);
        RefreshTotals();
        ResetAddProductSearch();
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    public async Task LoadFromDevisAsync(int devisId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(cancellationToken);
        Clients.Clear();
        foreach (var c in clients) Clients.Add(c);

        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);

        var d = await db.Devis.Include(x => x.Lignes).FirstAsync(x => x.Id == devisId, cancellationToken);
        DevisId = d.Id;
        ClientId = d.ClientId;
        Date = new DateTimeOffset(DateTime.Today);
        BlId = null;
        Numero = "(brouillon)";
        Lignes.Clear();
        ResetAddProductSearch();
        var catalogRefs = await DocumentLineCatalogLookups.LoadAsync(
            db,
            d.Lignes.Select(l => (l.ProduitId, l.ServiceId)),
            cancellationToken);
        foreach (var l in d.Lignes)
        {
            var row = new BLLineRow
            {
                ProduitId = l.ProduitId,
                ServiceId = l.ServiceId,
                Reference = catalogRefs.GetReference(l.ProduitId, l.ServiceId),
                Designation = l.Designation,
                Conditionnement = l.Conditionnement,
                QuantiteCommandee = l.Quantite,
                QuantiteLivree = l.Quantite,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA
            };
            row.PropertyChanged += LineOnPropertyChanged;
            Lignes.Add(row);
        }

        IsReadOnly = false;
        Title = _locale.T("BL_FromDevis");
        RefreshTotals();
    }

    public void LoadFromDevis(int devisId) => _ = LoadFromDevisAsync(devisId, CancellationToken.None);

    [RelayCommand]
    private void RemoveLine(BLLineRow? row)
    {
        if (!CanEdit || row == null) return;
        row.PropertyChanged -= LineOnPropertyChanged;
        Lignes.Remove(row);
    }

    private void RefreshTotals()
    {
        var includeTvaInTotals = ShowTotalTtc;
        var ht = Lignes.Sum(l => l.MontantHt);
        var tva = includeTvaInTotals
            ? Lignes.Sum(l => l.MontantHt * (l.TauxTva / 100m))
            : 0m;
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

    partial void OnDeviseChanged(string value) => RefreshTotals();

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
        if (!CanEdit)
        {
            await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), _locale.T("BL_ErrNoEdit"), cancellationToken);
            return;
        }

        if (ClientId == 0 || !Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), _locale.T("BL_ErrClientLines"), cancellationToken);
            return;
        }

        if (DocumentTotalsHelper.IsEffectivelyZeroTotal(TotalTtc))
        {
            await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), _locale.T("Doc_ErrZeroTtc"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            BonLivraison entity;
            if (BlId == null)
            {
                var num = await _numbers.NextBLAsync(cancellationToken);
                entity = new BonLivraison
                {
                    Numero = num,
                    ClientId = ClientId,
                    DevisId = DevisId,
                    ReservationId = ReservationId,
                    Date = Date.DateTime,
                    Note = BonCommandeReferenceStorage.Format(BonCommandeReference, Note),
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new BonLivraisonLigne
                    {
                        ProduitId = l.IsService ? null : l.ProduitId,
                        ServiceId = l.IsService ? l.ServiceId : null,
                        Designation = l.Designation,
                        QuantiteCommandee = l.QuantiteLivree,
                        QuantiteLivree = l.QuantiteLivree,
                        PrixUnitaireHT = l.PrixUnitaireHt,
                        Remise = l.Remise,
                        TauxTVA = l.TauxTva
                    });
                }

                db.BonsLivraison.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                BlId = entity.Id;
            }
            else
            {
                entity = await db.BonsLivraison.Include(b => b.Lignes).FirstAsync(b => b.Id == BlId, cancellationToken);
                entity.ClientId = ClientId;
                entity.DevisId = DevisId;
                entity.ReservationId = ReservationId;
                entity.Date = Date.DateTime;
                entity.Note = BonCommandeReferenceStorage.Format(BonCommandeReference, Note);
                entity.BonCommandeClientId = null;
                db.BonLivraisonLignes.RemoveRange(entity.Lignes);
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new BonLivraisonLigne
                    {
                        ProduitId = l.IsService ? null : l.ProduitId,
                        ServiceId = l.IsService ? l.ServiceId : null,
                        Designation = l.Designation,
                        QuantiteCommandee = l.QuantiteLivree,
                        QuantiteLivree = l.QuantiteLivree,
                        PrixUnitaireHT = l.PrixUnitaireHt,
                        Remise = l.Remise,
                        TauxTVA = l.TauxTva
                    });
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            try
            {
                await _workflow.ValiderAsync(entity.Id, _session.UserId, cancellationToken);
            }
            catch (Exception ex)
            {
                AppLog.Error("Échec de la validation du bon de livraison", ex, "BLEditViewModel.SaveAsync");
                await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), ex.Message, cancellationToken);
                await LoadAsync(BlId, cancellationToken);
                return;
            }

            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("BL_DlgShort"), _locale.T("BL_Saved"), cancellationToken);
            await LoadAsync(BlId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToFactureAsync(CancellationToken cancellationToken)
    {
        if (BlId == null) return;
        var factNum = await _blLinkService.GetInvoicingStatusAsync(BlId.Value, cancellationToken);
        if (factNum != null)
        {
            await _dialog.ShowErrorAsync(_locale.T("BL_DlgShort"), _locale.Tf("BL_ErrAlreadyInvoiced", factNum), cancellationToken);
            return;
        }

        var vm = _sp.GetRequiredService<FactureEditViewModel>();
        vm.LoadFromBL(BlId.Value);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void OpenLinkedFacture()
    {
        if (LinkedFactureId is not int factureId) return;
        var vm = _sp.GetRequiredService<FactureEditViewModel>();
        vm.Load(factureId);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void OpenLinkedReservation()
    {
        if (ReservationId is not int reservationId) return;
        var vm = _sp.GetRequiredService<ReservationEditViewModel>();
        vm.Load(reservationId);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<BLListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (BlId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBlPdfBytesAsync(cancellationToken);
            if (bytes == null) return;
            var ok = await _dialog.SavePickedFileBytesAsync(_locale.T("Export_PdfPicker"), $"{Numero}.pdf", new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Export_Done"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'export PDF du bon de livraison", ex, "BLEditViewModel.ExportPdfAsync");
            await _dialog.ShowErrorAsync(_locale.T("Export_Pdf"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrintAsync(CancellationToken cancellationToken)
    {
        if (BlId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBlPdfBytesAsync(cancellationToken);
            if (bytes == null) return;
            await _pdfPrint.PrintPdfAsync(bytes, Numero, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'impression du bon de livraison", ex, "BLEditViewModel.PrintAsync");
            await _dialog.ShowErrorAsync(_locale.T("Btn_Print"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<byte[]?> BuildBlPdfBytesAsync(CancellationToken cancellationToken)
    {
        if (BlId is not { } id) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var b = await db.BonsLivraison.Include(x => x.Lignes).FirstAsync(x => x.Id == id, cancellationToken);
        var client = await db.Tiers.AsNoTracking().FirstAsync(t => t.Id == b.ClientId, cancellationToken);
        return await _pdf.BuildBonLivraisonPdfAsync(b, DocumentPartyPdfInfo.FromTiers(client), cancellationToken);
    }
}
