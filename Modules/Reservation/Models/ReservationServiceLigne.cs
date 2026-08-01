using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Reservation.Models;

public class ReservationServiceLigne : BaseEntity
{
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public int? ServiceId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Note { get; set; } = string.Empty;
}
