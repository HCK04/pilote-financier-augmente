using PilotageFinancier.Domain;
using PilotageFinancier.Forecasting;
using Xunit;

namespace PilotageFinancier.Tests;

public class ForecasterTests
{
    private readonly ISsaForecaster _f = new SsaForecaster();

    private static List<PointSerie> Serie(int n, Func<int, float> f)
    {
        var start = new DateTime(2025, 1, 1);
        return Enumerable.Range(0, n).Select(i => new PointSerie(start.AddDays(i), f(i))).ToList();
    }

    [Fact]
    public void HistoriqueCourt_RetourneConfianceFaible()
    {
        // Arrange : moins que le seuil (12) -> pas d'échec, mode indicatif
        var serie = Serie(5, i => 100 + i);

        // Act
        var r = _f.Prevoir(serie, horizonPeriods: 3, TypeSerie.Tresorerie, SsaConfig.Tresorerie);

        // Assert
        Assert.Equal(NiveauConfiance.Faible, r.Confiance);
        Assert.Equal(3, r.Points.Count);
        Assert.NotNull(r.Message);
    }

    [Fact]
    public void HistoriqueSuffisant_RetourneConfianceNormale_EtBonNombreDePoints()
    {
        var serie = Serie(36, i => 100 + 10 * MathF.Sin(i / 3f));

        var r = _f.Prevoir(serie, horizonPeriods: 6, TypeSerie.Tresorerie, SsaConfig.Tresorerie);

        Assert.Equal(NiveauConfiance.Normale, r.Confiance);
        Assert.Equal(6, r.Points.Count);
    }

    [Fact]
    public void Budgetaire_DepassementPlafond_LeveAlerte()
    {
        // Série cumulée croissante qui va dépasser un plafond bas
        var serie = Serie(24, i => 1000f * (i + 1));

        var r = _f.Prevoir(serie, horizonPeriods: 6, TypeSerie.Budgetaire, SsaConfig.Budgetaire, plafond: 26000m);

        Assert.Equal(TypeSerie.Budgetaire, r.TypeSerie);
        Assert.Contains(r.Points, p => p.AlerteDepassement);
    }

    [Fact]
    public void Budgetaire_SousPlafond_AucuneAlerte()
    {
        var serie = Serie(24, i => 100f * (i + 1));

        var r = _f.Prevoir(serie, horizonPeriods: 3, TypeSerie.Budgetaire, SsaConfig.Budgetaire, plafond: 10_000_000m);

        Assert.DoesNotContain(r.Points, p => p.AlerteDepassement);
    }

    [Fact]
    public void HorizonInvalide_Leve()
    {
        var serie = Serie(20, i => i);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _f.Prevoir(serie, 0, TypeSerie.Tresorerie, SsaConfig.Tresorerie));
    }
}
