namespace GestionCommerciale.Shared.Services;

public sealed class AvailabilityWarningDialogModel
{
    public required string Title { get; init; }
    public required string Header { get; init; }
    public required string PeriodText { get; init; }
    public required string ConfirmQuestion { get; init; }
    public string YesLabel { get; init; } = "Oui";
    public string NoLabel { get; init; } = "Non";
    public required IReadOnlyList<AvailabilityWarningProductBlock> Products { get; init; }
}

public sealed class AvailabilityWarningProductBlock
{
    public required string ProductTitle { get; init; }
    public required string DemandeLabel { get; init; }
    public required string DemandeValue { get; init; }
    public required string DisponibleLabel { get; init; }
    public required string DisponibleValue { get; init; }
    public required string StockLabel { get; init; }
    public required string StockValue { get; init; }
    public required string DejaLabel { get; init; }
    public required string DejaValue { get; init; }
    public string? ConflictsHeader { get; init; }
    public IReadOnlyList<AvailabilityWarningConflictChip> Conflicts { get; init; } = [];
}

public sealed class AvailabilityWarningConflictChip
{
    public required string Title { get; init; }
    public required string Detail { get; init; }
}
