using GestionCommerciale.Modules.Location.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Location.ViewModels;

public static class LocationStatutLabels
{
    public static string Format(ILocaleService locale, StatutLocation s) =>
        locale.T(s switch
        {
            StatutLocation.Brouillon => "Loc_Statut_Brouillon",
            StatutLocation.EnCours => "Loc_Statut_EnCours",
            StatutLocation.PartiellementRetournee => "Loc_Statut_Partiel",
            StatutLocation.Retournee => "Loc_Statut_Retournee",
            StatutLocation.Annulee => "Loc_Statut_Annulee",
            _ => "Loc_Statut_Brouillon"
        });

    public static IReadOnlyList<StatutLocation> All { get; } =
    [
        StatutLocation.Brouillon,
        StatutLocation.EnCours,
        StatutLocation.PartiellementRetournee,
        StatutLocation.Retournee,
        StatutLocation.Annulee
    ];
}
