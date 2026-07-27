using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Location.ViewModels;

public partial class LocationLineRow : ObservableObject
{
    private static readonly IBrush BlueBg = Brush.Parse("#DBEAFE");
    private static readonly IBrush BlueBorder = Brush.Parse("#93C5FD");
    private static readonly IBrush BlueFg = Brush.Parse("#1E40AF");
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

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise);

    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    /// <summary>True when returned qty covers rented qty.</summary>
    public bool IsRetourComplet => Quantite > 0 && QuantiteRetournee >= Quantite;

    public IBrush QteLoueeBackground => BlueBg;
    public IBrush QteLoueeBorder => BlueBorder;
    public IBrush QteLoueeForeground => BlueFg;

    public IBrush QteRetourBackground => IsRetourComplet ? BlueBg : YellowBg;
    public IBrush QteRetourBorder => IsRetourComplet ? BlueBorder : YellowBorder;
    public IBrush QteRetourForeground => IsRetourComplet ? BlueFg : YellowFg;

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnQuantiteRetourneeChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();

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
        Reference = item.Reference;
        Designation = item.Designation;
        PrixUnitaireHt = item.PrixLocationHT > 0 ? item.PrixLocationHT : item.PrixVenteHT;
        TauxTva = item.TauxTVA;
        NotifyMontants();
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
        OnPropertyChanged(nameof(QuantiteEncoreSortie));
        OnPropertyChanged(nameof(IsRetourComplet));
        OnPropertyChanged(nameof(QteRetourBackground));
        OnPropertyChanged(nameof(QteRetourBorder));
        OnPropertyChanged(nameof(QteRetourForeground));
    }
}
