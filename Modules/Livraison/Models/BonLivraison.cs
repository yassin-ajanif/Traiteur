using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Models;
using ReservationEntity = GestionCommerciale.Modules.Reservation.Models.Reservation;

namespace GestionCommerciale.Modules.Livraison.Models;

public class BonLivraison : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public int? DevisId { get; set; }
    public int? BonCommandeClientId { get; set; }
    public int? ReservationId { get; set; }
    public ReservationEntity? Reservation { get; set; }
    public int? FactureId { get; set; }
    public Facture? Facture { get; set; }
    public DateTime Date { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<BonLivraisonLigne> Lignes { get; set; } = [];
}
