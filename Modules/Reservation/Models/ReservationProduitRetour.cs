using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class ReservationProduitRetour : BaseEntity
{
    public int ReservationProduitLigneId { get; set; }
    public ReservationProduitLigne? ReservationProduitLigne { get; set; }
    public DateTime DateRetour { get; set; }
    public decimal Quantite { get; set; }
    /// <summary>Condition on return: <see cref="ReservationProduitRetourEtats"/>.</summary>
    public string Etat { get; set; } = ReservationProduitRetourEtats.Good;
    public string Note { get; set; } = string.Empty;
}
