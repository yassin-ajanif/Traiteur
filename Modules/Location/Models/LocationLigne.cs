using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Location.Models;

public class LocationLigne : BaseEntity
{
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public int? ProduitId { get; set; }
    public int? ServiceId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal QuantiteRetournee { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Note { get; set; } = string.Empty;
}
