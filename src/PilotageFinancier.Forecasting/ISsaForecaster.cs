using PilotageFinancier.Domain;

namespace PilotageFinancier.Forecasting;

public interface ISsaForecaster
{
    ResultatPrevision Prevoir(
        IReadOnlyList<PointSerie> historique,
        int horizonPeriods,
        TypeSerie typeSerie,
        SsaConfig config,
        decimal? plafond = null);
}
