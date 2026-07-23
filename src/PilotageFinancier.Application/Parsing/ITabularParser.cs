namespace PilotageFinancier.Application.Parsing;

/// <summary>Lit un fichier tabulaire (CSV ou Excel) en lignes de cellules, en-tête exclu.</summary>
public interface ITabularParser
{
    /// <summary>Retourne les lignes de données (hors en-tête). Chaque ligne = tableau de cellules texte.</summary>
    IReadOnlyList<string[]> Lire(Stream flux, string nomFichier);
}
