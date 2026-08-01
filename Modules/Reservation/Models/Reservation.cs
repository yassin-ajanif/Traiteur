using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class Reservation : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFinPrevue { get; set; }
    public DateTime? DateRetourEffective { get; set; }
    public StatutReservation Statut { get; set; } = StatutReservation.EnCours;
    public decimal Caution { get; set; }
    public decimal RemiseGlobale { get; set; }
    public string Note { get; set; } = string.Empty;
    public int? FactureId { get; set; }
    public Facture? Facture { get; set; }
    /// <summary>Linked delivery note (Vers BL). Stock stays on Reservation.</summary>
    public int? BonLivraisonId { get; set; }
    public BonLivraison? BonLivraison { get; set; }
    public List<ReservationProduitLigne> ProduitLignes { get; set; } = [];
    public List<ReservationServiceLigne> ServiceLignes { get; set; } = [];
}
