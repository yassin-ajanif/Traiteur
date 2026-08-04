using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public sealed class RetourEtatOption
{
    private static readonly IBrush GoodBg = Brush.Parse("#DCFCE7");
    private static readonly IBrush GoodBorder = Brush.Parse("#86EFAC");
    private static readonly IBrush GoodFg = Brush.Parse("#166534");

    private static readonly IBrush BadBg = Brush.Parse("#FEE2E2");
    private static readonly IBrush BadBorder = Brush.Parse("#FCA5A5");
    private static readonly IBrush BadFg = Brush.Parse("#991B1B");

    private static readonly IBrush CleanBg = Brush.Parse("#FEF3C7");
    private static readonly IBrush CleanBorder = Brush.Parse("#FCD34D");
    private static readonly IBrush CleanFg = Brush.Parse("#92400E");

    public required string Value { get; init; }
    public required string Label { get; init; }

    public IBrush Background => ColorsFor(Value).Bg;
    public IBrush BorderBrush => ColorsFor(Value).Border;
    public IBrush Foreground => ColorsFor(Value).Fg;

    public override string ToString() => Label;

    private static (IBrush Bg, IBrush Border, IBrush Fg) ColorsFor(string value) => value switch
    {
        ReservationProduitRetourEtats.Good => (GoodBg, GoodBorder, GoodFg),
        ReservationProduitRetourEtats.Damaged or ReservationProduitRetourEtats.Lost => (BadBg, BadBorder, BadFg),
        ReservationProduitRetourEtats.ToClean => (CleanBg, CleanBorder, CleanFg),
        _ => (GoodBg, GoodBorder, GoodFg)
    };
}
