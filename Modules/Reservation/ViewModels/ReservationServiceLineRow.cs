using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class ReservationServiceLineRow : ObservableObject
{
    [ObservableProperty] private int? _serviceId;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private decimal _quantite;
    [ObservableProperty] private decimal _prixUnitaireHt;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTva;
    [ObservableProperty] private string _note = string.Empty;

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise);

    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();

    public void ApplyCatalogItem(DocumentCatalogItem item)
    {
        ServiceId = item.Id;
        PrixUnitaireHt = item.PrixVenteHT;
        Reference = item.Reference;
        Designation = item.Designation;
        TauxTva = item.TauxTVA;
        NotifyMontants();
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
    }
}
