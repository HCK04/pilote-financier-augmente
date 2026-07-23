using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using PilotageFinancier.Domain;

namespace PilotageFinancier.Forecasting;

/// <summary>
/// Moteur de prévision reposant sur SSA (ML.NET). Une architecture commune, deux configurations :
/// fenêtre courte pour la trésorerie, fenêtre longue (+ contrainte de plafond) pour le budgétaire.
/// Robuste aux historiques courts : sous le seuil, bascule sur une projection indicative.
/// </summary>
public class SsaForecaster : ISsaForecaster
{
    private sealed class SsaInput { public float Valeur { get; set; } }
    private sealed class SsaOutput
    {
        [VectorType] public float[] Prevision { get; set; } = [];
        [VectorType] public float[] BorneInf { get; set; } = [];
        [VectorType] public float[] BorneSup { get; set; } = [];
    }

    /// <summary>
    /// Produit une prévision à <paramref name="horizonPeriods"/> périodes.
    /// L'horizon est un paramètre d'inférence : il ne conditionne pas la structure des données.
    /// </summary>
    public ResultatPrevision Prevoir(
        IReadOnlyList<PointSerie> historique,
        int horizonPeriods,
        TypeSerie typeSerie,
        SsaConfig config,
        decimal? plafond = null)
    {
        if (horizonPeriods < 1) throw new ArgumentOutOfRangeException(nameof(horizonPeriods));
        var n = historique.Count;
        var derniere = n > 0 ? historique[^1].Periode : DateTime.UtcNow;
        var pas = InfererPas(historique);

        // Historique insuffisant -> prévision indicative (jamais d'échec silencieux).
        if (n < config.SeuilMinPoints)
        {
            var pts = ProjectionIndicative(historique, horizonPeriods, derniere, pas);
            var indic = AppliquerPlafond(pts, typeSerie, plafond);
            return new ResultatPrevision(typeSerie, NiveauConfiance.Faible, indic,
                $"Historique court ({n} points < {config.SeuilMinPoints}) : prévision indicative, confiance faible.",
                plafond);
        }

        // Fenêtre SSA bornée par la profondeur d'historique disponible.
        var window = Math.Max(2, Math.Min(config.WindowSize, (n / 2) - 1));
        var ml = new MLContext(seed: 1);
        var data = ml.Data.LoadFromEnumerable(historique.Select(p => new SsaInput { Valeur = p.Valeur }));

        var pipeline = ml.Forecasting.ForecastBySsa(
            outputColumnName: nameof(SsaOutput.Prevision),
            inputColumnName: nameof(SsaInput.Valeur),
            windowSize: window,
            seriesLength: n,
            trainSize: n,
            horizon: horizonPeriods,
            confidenceLevel: config.ConfidenceLevel,
            confidenceLowerBoundColumn: nameof(SsaOutput.BorneInf),
            confidenceUpperBoundColumn: nameof(SsaOutput.BorneSup));

        var model = pipeline.Fit(data);
        var engine = model.CreateTimeSeriesEngine<SsaInput, SsaOutput>(ml);
        var forecast = engine.Predict();

        var points = new List<PrevisionPoint>();
        for (var i = 0; i < horizonPeriods; i++)
        {
            var periode = Avancer(derniere, pas, i + 1);
            points.Add(new PrevisionPoint(
                periode,
                forecast.Prevision[i],
                forecast.BorneInf.Length > i ? forecast.BorneInf[i] : forecast.Prevision[i],
                forecast.BorneSup.Length > i ? forecast.BorneSup[i] : forecast.Prevision[i]));
        }

        var finaux = AppliquerPlafond(points, typeSerie, plafond);
        return new ResultatPrevision(typeSerie, NiveauConfiance.Normale, finaux, Plafond: plafond);
    }

    /// <summary>
    /// Règle métier budgétaire : la prévision cumulée ne doit jamais dépasser silencieusement
    /// l'enveloppe votée. Au-delà du plafond, on lève une alerte de dépassement anticipé.
    /// </summary>
    private static IReadOnlyList<PrevisionPoint> AppliquerPlafond(
        IReadOnlyList<PrevisionPoint> points, TypeSerie typeSerie, decimal? plafond)
    {
        if (typeSerie != TypeSerie.Budgetaire || plafond is null) return points;
        var seuil = (float)plafond.Value;
        return points.Select(p => p with { AlerteDepassement = p.Valeur > seuil }).ToList();
    }

    /// <summary>Projection de repli quand l'historique est trop court pour SSA : tendance linéaire simple.</summary>
    private static IReadOnlyList<PrevisionPoint> ProjectionIndicative(
        IReadOnlyList<PointSerie> h, int horizon, DateTime derniere, TimeSpan pas)
    {
        var points = new List<PrevisionPoint>();
        if (h.Count == 0)
        {
            for (var i = 0; i < horizon; i++)
                points.Add(new PrevisionPoint(Avancer(derniere, pas, i + 1), 0, 0, 0));
            return points;
        }
        // Pente moyenne des différences successives.
        float pente = 0;
        for (var i = 1; i < h.Count; i++) pente += h[i].Valeur - h[i - 1].Valeur;
        pente = h.Count > 1 ? pente / (h.Count - 1) : 0;
        var derniereValeur = h[^1].Valeur;
        for (var i = 0; i < horizon; i++)
        {
            var v = derniereValeur + pente * (i + 1);
            points.Add(new PrevisionPoint(Avancer(derniere, pas, i + 1), v, v, v));
        }
        return points;
    }

    private static TimeSpan InfererPas(IReadOnlyList<PointSerie> h)
    {
        if (h.Count < 2) return TimeSpan.FromDays(1);
        return h[^1].Periode - h[^2].Periode;
    }

    private static DateTime Avancer(DateTime baseDate, TimeSpan pas, int n) => baseDate + pas * n;
}
