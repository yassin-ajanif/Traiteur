using Avalonia.Media;
using GestionCommerciale.Modules.Location.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Location.ViewModels;

public static class LocationStatutLabels
{
    public static string Format(ILocaleService locale, StatutLocation s) =>
        locale.T(s switch
        {
            StatutLocation.EnCours => "Loc_Statut_EnCours",
            StatutLocation.PartiellementRetournee => "Loc_Statut_Partiel",
            StatutLocation.Retournee => "Loc_Statut_Retournee",
            _ => "Loc_Statut_EnCours"
        });

    public static StatutLocation FromQuantites(IEnumerable<(decimal Quantite, decimal QuantiteRetournee)> lines)
    {
        var list = lines as IList<(decimal Quantite, decimal QuantiteRetournee)> ?? lines.ToList();
        if (list.Count == 0)
            return StatutLocation.EnCours;

        var anyOut = list.Any(l => l.Quantite > l.QuantiteRetournee);
        var anyReturned = list.Any(l => l.QuantiteRetournee > 0);
        if (!anyOut)
            return StatutLocation.Retournee;
        if (anyReturned)
            return StatutLocation.PartiellementRetournee;
        return StatutLocation.EnCours;
    }

    /// <summary>Maps legacy DB values (0=Brouillon, 4=Annulée) to a valid statut.</summary>
    public static StatutLocation Normalize(StatutLocation stored) =>
        stored is StatutLocation.EnCours
            or StatutLocation.PartiellementRetournee
            or StatutLocation.Retournee
            ? stored
            : StatutLocation.EnCours;

    public static IBrush ChipBackground(StatutLocation s) => s switch
    {
        StatutLocation.Retournee => Brush.Parse("#DCFCE7"),
        StatutLocation.PartiellementRetournee => Brush.Parse("#FEF3C7"),
        _ => Brush.Parse("#DBEAFE")
    };

    public static IBrush ChipForeground(StatutLocation s) => s switch
    {
        StatutLocation.Retournee => Brush.Parse("#166534"),
        StatutLocation.PartiellementRetournee => Brush.Parse("#92400E"),
        _ => Brush.Parse("#1E40AF")
    };

    public static IBrush ChipBorder(StatutLocation s) => s switch
    {
        StatutLocation.Retournee => Brush.Parse("#86EFAC"),
        StatutLocation.PartiellementRetournee => Brush.Parse("#FCD34D"),
        _ => Brush.Parse("#93C5FD")
    };
}
