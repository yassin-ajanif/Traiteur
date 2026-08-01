using System.Globalization;
using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public sealed class ReservationListRow
{
    public required Models.Reservation Reservation { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string PeriodeLabel { get; init; } = string.Empty;
    public string StatutLabel { get; init; } = string.Empty;
    public IBrush StatutChipBackground { get; init; } = Brushes.Transparent;
    public IBrush StatutChipForeground { get; init; } = Brushes.Black;
    public IBrush StatutChipBorder { get; init; } = Brushes.Transparent;
    public string TtcLabel { get; init; } = string.Empty;
    public string NotePreview { get; init; } = string.Empty;

    public static ReservationListRow Create(Models.Reservation res, string clientNom, string devise, ILocaleService locale)
    {
        var produitLignes = res.ProduitLignes ?? [];
        var statut = ReservationStatutLabels.FromQuantites(
            produitLignes.Select(l => (l.Quantite, l.QuantiteRetournee)));
        var (_, _, ttc) = DocumentTotalsHelper.ReservationTotals(produitLignes, res.ServiceLignes ?? [], res.RemiseGlobale);
        return new ReservationListRow
        {
            Reservation = res,
            ClientNom = clientNom,
            DateShort = res.Date.ToString("d", CultureInfo.CurrentCulture),
            PeriodeLabel = $"{res.DateDebut:dd/MM} → {res.DateFinPrevue:dd/MM}",
            StatutLabel = ReservationStatutLabels.Format(locale, statut),
            StatutChipBackground = ReservationStatutLabels.ChipBackground(statut),
            StatutChipForeground = ReservationStatutLabels.ChipForeground(statut),
            StatutChipBorder = ReservationStatutLabels.ChipBorder(statut),
            TtcLabel = $"{ttc:N2} {devise}",
            NotePreview = DocumentListFormat.NotePreview(res.Note),
        };
    }
}
