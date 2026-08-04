using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Reservation.Models;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public partial class ReservationRetourRow : ObservableObject
{
    public ReservationProduitLineRow Line { get; }

    [ObservableProperty] private DateTime _dateRetour = DateTime.Today;
    [ObservableProperty] private decimal _quantite;
    [ObservableProperty] private string _etat = ReservationProduitRetourEtats.Good;
    [ObservableProperty] private RetourEtatOption? _etatOption;
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

    public void SyncEtatOption(IEnumerable<RetourEtatOption> options)
    {
        var match = options.FirstOrDefault(o => o.Value == Etat)
                    ?? options.FirstOrDefault(o => o.Value == ReservationProduitRetourEtats.Good)
                    ?? options.FirstOrDefault();
        if (!ReferenceEquals(EtatOption, match))
            EtatOption = match;
        if (match != null && Etat != match.Value)
            Etat = match.Value;
    }

    partial void OnEtatOptionChanged(RetourEtatOption? value)
    {
        if (value != null && Etat != value.Value)
            Etat = value.Value;
    }
}
