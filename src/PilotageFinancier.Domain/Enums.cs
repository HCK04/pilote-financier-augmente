namespace PilotageFinancier.Domain;

/// <summary>Nature de la série temporelle prévue.</summary>
public enum TypeSerie
{
    Tresorerie = 0,
    Budgetaire = 1
}

/// <summary>Granularité d'agrégation d'une série.</summary>
public enum Granularite
{
    Jour = 0,
    Semaine = 1,
    Mois = 2
}

/// <summary>Type de fichier importé par le client.</summary>
public enum TypeImport
{
    EcrituresComptables = 0,
    BudgetVote = 1
}

/// <summary>Niveau de confiance d'une prévision, selon la profondeur d'historique.</summary>
public enum NiveauConfiance
{
    /// <summary>Historique insuffisant (&lt; seuil) : prévision indicative.</summary>
    Faible = 0,
    Normale = 1
}
