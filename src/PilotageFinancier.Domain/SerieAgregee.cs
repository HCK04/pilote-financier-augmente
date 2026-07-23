namespace PilotageFinancier.Domain;

/// <summary>
/// Point d'une série temporelle agrégée (cache de calcul).
/// Recalculé à chaque import. Seule entrée du moteur de prévision.
/// </summary>
public class SerieAgregee
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }

    public TypeSerie TypeSerie { get; set; }
    public Granularite Granularite { get; set; }

    /// <summary>Début de la période agrégée (jour, lundi de la semaine, ou 1er du mois).</summary>
    public DateTime Periode { get; set; }
    public decimal Valeur { get; set; }

    /// <summary>Code PCGE pour la série budgétaire (null pour la trésorerie globale).</summary>
    public string? CodePCGE { get; set; }
}
