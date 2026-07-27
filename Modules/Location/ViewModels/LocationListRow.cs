using System.Globalization;
using Avalonia.Media;
using GestionCommerciale.Modules.Location.Models;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Location.ViewModels;

public sealed class LocationListRow
{
    public required Models.Location Location { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string PeriodeLabel { get; init; } = string.Empty;
    public string StatutLabel { get; init; } = string.Empty;
    public IBrush StatutChipBackground { get; init; } = Brushes.Transparent;
    public IBrush StatutChipForeground { get; init; } = Brushes.Black;
    public IBrush StatutChipBorder { get; init; } = Brushes.Transparent;
    public string TtcLabel { get; init; } = string.Empty;
    public string NotePreview { get; init; } = string.Empty;

    public static LocationListRow Create(Models.Location loc, string clientNom, string devise, ILocaleService locale)
    {
        var statut = LocationStatutLabels.FromQuantites(
            (loc.Lignes ?? []).Select(l => (l.Quantite, l.QuantiteRetournee)));
        var (_, _, ttc) = DocumentTotalsHelper.LocationTotals(loc.Lignes ?? [], loc.RemiseGlobale);
        return new LocationListRow
        {
            Location = loc,
            ClientNom = clientNom,
            DateShort = loc.Date.ToString("d", CultureInfo.CurrentCulture),
            PeriodeLabel = $"{loc.DateDebut:dd/MM} → {loc.DateFinPrevue:dd/MM}",
            StatutLabel = LocationStatutLabels.Format(locale, statut),
            StatutChipBackground = LocationStatutLabels.ChipBackground(statut),
            StatutChipForeground = LocationStatutLabels.ChipForeground(statut),
            StatutChipBorder = LocationStatutLabels.ChipBorder(statut),
            TtcLabel = $"{ttc:N2} {devise}",
            NotePreview = DocumentListFormat.NotePreview(loc.Note),
        };
    }
}
