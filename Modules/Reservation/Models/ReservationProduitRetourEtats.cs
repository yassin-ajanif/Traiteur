namespace GestionCommerciale.Modules.Reservation.Models;

/// <summary>Allowed string values for <see cref="ReservationProduitRetour.Etat"/> (DB check constraint).</summary>
public static class ReservationProduitRetourEtats
{
    public const string Good = "good";
    public const string Damaged = "damaged";
    public const string Lost = "lost";
    public const string ToClean = "to clean";

    public static readonly IReadOnlyList<string> All =
    [
        Good,
        Damaged,
        Lost,
        ToClean
    ];

    public static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) && All.Contains(value);
}
