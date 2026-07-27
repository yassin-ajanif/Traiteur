using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Location.Models;

public class Location : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFinPrevue { get; set; }
    public DateTime? DateRetourEffective { get; set; }
    public StatutLocation Statut { get; set; } = StatutLocation.EnCours;
    public decimal Caution { get; set; }
    public decimal RemiseGlobale { get; set; }
    public string Note { get; set; } = string.Empty;
    public int? FactureId { get; set; }
    public Facture? Facture { get; set; }
    /// <summary>Linked delivery note (Vers BL). Stock stays on Location.</summary>
    public int? BonLivraisonId { get; set; }
    public BonLivraison? BonLivraison { get; set; }
    public List<LocationLigne> Lignes { get; set; } = [];
}
