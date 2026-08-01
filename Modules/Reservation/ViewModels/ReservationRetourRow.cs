using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class ReservationRetourRow : ObservableObject
{
    public ReservationProduitLineRow Line { get; }

    [ObservableProperty] private DateTime _dateRetour = DateTime.Today;
    [ObservableProperty] private decimal _quantite;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private decimal _maxQuantite = 999_999m;

    public ReservationRetourRow(ReservationProduitLineRow line)
    {
        Line = line;
    }

    public string ProduitLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Line.Reference) && !string.IsNullOrWhiteSpace(Line.Designation))
                return $"{Line.Reference} — {Line.Designation}";
            return string.IsNullOrWhiteSpace(Line.Designation) ? Line.Reference : Line.Designation;
        }
    }

    public void NotifyProduitLabel() => OnPropertyChanged(nameof(ProduitLabel));
}
