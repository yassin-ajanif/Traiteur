using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class ReservationProduitLineRow : ObservableObject
{
    private static readonly IBrush GoldBg = Brush.Parse("#F5E9C8");
    private static readonly IBrush GoldBorder = Brush.Parse("#C4A035");
    private static readonly IBrush GoldFg = Brush.Parse("#8A7020");
    private static readonly IBrush YellowBg = Brush.Parse("#FEF3C7");
    private static readonly IBrush YellowBorder = Brush.Parse("#FCD34D");
    private static readonly IBrush YellowFg = Brush.Parse("#92400E");

    [ObservableProperty] private int? _produitId;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private decimal _quantite;
    [ObservableProperty] private decimal _quantiteRetournee;
    [ObservableProperty] private decimal _prixUnitaireHt;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTva;
    [ObservableProperty] private string _note = string.Empty;

    public decimal QuantiteEncoreSortie => Math.Max(0, Quantite - QuantiteRetournee);

    public string RetourOptionLabel
    {
        get
        {
            var name = !string.IsNullOrWhiteSpace(Reference) && !string.IsNullOrWhiteSpace(Designation)
                ? $"{Reference} — {Designation}"
                : (string.IsNullOrWhiteSpace(Designation) ? Reference : Designation);
            return $"{name} (encore {QuantiteEncoreSortie:N2})";
        }
    }

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise);

    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    public bool IsRetourComplet => Quantite > 0 && QuantiteRetournee >= Quantite;

    public IBrush QteLoueeBackground => GoldBg;
    public IBrush QteLoueeBorder => GoldBorder;
    public IBrush QteLoueeForeground => GoldFg;

    public IBrush QteRetourBackground => IsRetourComplet ? GoldBg : YellowBg;
    public IBrush QteRetourBorder => IsRetourComplet ? GoldBorder : YellowBorder;
    public IBrush QteRetourForeground => IsRetourComplet ? GoldFg : YellowFg;

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnQuantiteRetourneeChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();
    partial void OnReferenceChanged(string value) => OnPropertyChanged(nameof(RetourOptionLabel));
    partial void OnDesignationChanged(string value) => OnPropertyChanged(nameof(RetourOptionLabel));

    public void ApplyCatalogProduct(Produit p)
    {
        ProduitId = p.Id;
        Reference = p.Reference;
        Designation = p.Designation;
        PrixUnitaireHt = p.PrixLocationHT > 0 ? p.PrixLocationHT : p.PrixVenteHT;
        TauxTva = p.TauxTVA;
        NotifyMontants();
    }

    public void ApplyCatalogItem(DocumentCatalogItem item)
    {
        ProduitId = item.Id;
        PrixUnitaireHt = item.PrixLocationHT > 0 ? item.PrixLocationHT : item.PrixVenteHT;
        Reference = item.Reference;
        Designation = item.Designation;
        TauxTva = item.TauxTVA;
        NotifyMontants();
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
        OnPropertyChanged(nameof(QuantiteEncoreSortie));
        OnPropertyChanged(nameof(RetourOptionLabel));
        OnPropertyChanged(nameof(IsRetourComplet));
        OnPropertyChanged(nameof(QteRetourBackground));
        OnPropertyChanged(nameof(QteRetourBorder));
        OnPropertyChanged(nameof(QteRetourForeground));
    }
}
