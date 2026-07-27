using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Location.ViewModels;

public sealed partial class EtatClientRow : ObservableObject
{
    public int ClientId { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string ClientTelephone { get; init; } = string.Empty;
    public int NbLocationsOuvertes { get; init; }
    public decimal QteEncoreSortie { get; init; }
    public string QteEncoreLabel { get; init; } = string.Empty;
    public DateTime? ProchaineFinPrevue { get; init; }
    public string FinPrevueLabel { get; init; } = string.Empty;
    public bool EstEnRetard { get; init; }
    public string RetardBadge { get; init; } = string.Empty;
    public decimal CautionTotale { get; init; }
    public string CautionLabel { get; init; } = string.Empty;
    public string SummaryLabel { get; init; } = string.Empty;

    [ObservableProperty] private bool _isExpanded;

    public ObservableCollection<EtatClientItemRow> Items { get; } = [];
}

public sealed class EtatClientItemRow
{
    public int LocationId { get; init; }
    public string LocationNumero { get; init; } = string.Empty;
    public string ProduitReference { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public decimal QuantiteLouee { get; init; }
    public decimal QuantiteRetournee { get; init; }
    public decimal QuantiteEncore { get; init; }
    public string QuantiteEncoreLabel { get; init; } = string.Empty;
    public DateTime DateDebut { get; init; }
    public DateTime DateFinPrevue { get; init; }
    public string PeriodeLabel { get; init; } = string.Empty;
    public bool EstEnRetard { get; init; }
    public string RetardLabel { get; init; } = string.Empty;
    public string MetaLabel { get; init; } = string.Empty;
}
