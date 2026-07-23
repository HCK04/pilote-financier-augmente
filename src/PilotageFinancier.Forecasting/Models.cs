using PilotageFinancier.Domain;

namespace PilotageFinancier.Forecasting;

/// <summary>Point d'entrée d'une série (valeur observée sur une période).</summary>
public record PointSerie(DateTime Periode, float Valeur);

/// <summary>Configuration d'un pipeline SSA (courte pour trésorerie, longue pour budgétaire).</summary>
public record SsaConfig
{
    /// <summary>Taille de la fenêtre saisonnière SSA.</summary>
    public int WindowSize { get; init; } = 7;
    /// <summary>Niveau de confiance des bornes (0-1).</summary>
    public float ConfidenceLevel { get; init; } = 0.95f;
    /// <summary>Seuil d'historique en dessous duquel on bascule en mode indicatif.</summary>
    public int SeuilMinPoints { get; init; } = 12;

    public static SsaConfig Tresorerie => new() { WindowSize = 7, SeuilMinPoints = 12 };
    public static SsaConfig Budgetaire => new() { WindowSize = 4, SeuilMinPoints = 12, ConfidenceLevel = 0.90f };
}

/// <summary>Un point prévu, avec bornes de confiance et éventuelle alerte de dépassement.</summary>
public record PrevisionPoint(
    DateTime Periode,
    float Valeur,
    float BorneInf,
    float BorneSup,
    bool AlerteDepassement = false);

/// <summary>Résultat complet d'une prévision.</summary>
public record ResultatPrevision(
    TypeSerie TypeSerie,
    NiveauConfiance Confiance,
    IReadOnlyList<PrevisionPoint> Points,
    string? Message = null,
    decimal? Plafond = null);
