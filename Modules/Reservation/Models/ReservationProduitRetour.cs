using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class ReservationProduitRetour : BaseEntity
{
    public int ReservationProduitLigneId { get; set; }
    public ReservationProduitLigne? ReservationProduitLigne { get; set; }
    public DateTime DateRetour { get; set; }
    public decimal Quantite { get; set; }
    public string Note { get; set; } = string.Empty;
}
