using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class ReservationProduitLigne : BaseEntity
{
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public int? ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    /// <summary>Sum of <see cref="Retours"/>; kept denormalized for stock/status/Etat client.</summary>
    public decimal QuantiteRetournee { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Note { get; set; } = string.Empty;
    public ICollection<ReservationProduitRetour> Retours { get; set; } = new List<ReservationProduitRetour>();
}
