namespace PilotageFinancier.Domain;

/// <summary>
/// Ligne de budget voté, versionnée par DateValidite pour tracer les réajustements
/// (reventilation / virements de crédits en cours d'exercice).
/// </summary>
public class BudgetVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public string CodePCGE { get; set; } = string.Empty;
    public int Exercice { get; set; }
    public decimal MontantVote { get; set; }

    /// <summary>Date de validité de cette version de la ligne budgétaire.</summary>
    public DateTime DateValidite { get; set; } = DateTime.UtcNow;
}
