using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public static class ReservationStatutLabels
{
    public static string Format(ILocaleService locale, StatutReservation s) =>
        locale.T(s switch
        {
            StatutReservation.EnCours => "Loc_Statut_EnCours",
            StatutReservation.PartiellementRetournee => "Loc_Statut_Partiel",
            StatutReservation.Retournee => "Loc_Statut_Retournee",
            _ => "Loc_Statut_EnCours"
        });

    /// <summary>Status is driven by product lines only (services are not returnable).</summary>
    public static StatutReservation FromQuantites(IEnumerable<(decimal Quantite, decimal QuantiteRetournee)> productLines)
    {
        var list = productLines as IList<(decimal Quantite, decimal QuantiteRetournee)> ?? productLines.ToList();
        if (list.Count == 0)
            return StatutReservation.EnCours;

        var anyOut = list.Any(l => l.Quantite > l.QuantiteRetournee);
        var anyReturned = list.Any(l => l.QuantiteRetournee > 0);
        if (!anyOut)
            return StatutReservation.Retournee;
        if (anyReturned)
            return StatutReservation.PartiellementRetournee;
        return StatutReservation.EnCours;
    }

    public static StatutReservation Normalize(StatutReservation stored) =>
        stored is StatutReservation.EnCours
            or StatutReservation.PartiellementRetournee
            or StatutReservation.Retournee
            ? stored
            : StatutReservation.EnCours;

    public static IBrush ChipBackground(StatutReservation s) => s switch
    {
        StatutReservation.Retournee => Brush.Parse("#DCFCE7"),
        StatutReservation.PartiellementRetournee => Brush.Parse("#FEF3C7"),
        _ => Brush.Parse("#F5E9C8")
    };

    public static IBrush ChipForeground(StatutReservation s) => s switch
    {
        StatutReservation.Retournee => Brush.Parse("#166534"),
        StatutReservation.PartiellementRetournee => Brush.Parse("#92400E"),
        _ => Brush.Parse("#8A7020")
    };

    public static IBrush ChipBorder(StatutReservation s) => s switch
    {
        StatutReservation.Retournee => Brush.Parse("#86EFAC"),
        StatutReservation.PartiellementRetournee => Brush.Parse("#FCD34D"),
        _ => Brush.Parse("#C4A035")
    };
}
