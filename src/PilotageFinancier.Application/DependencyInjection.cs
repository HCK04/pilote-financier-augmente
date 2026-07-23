using Microsoft.Extensions.DependencyInjection;
using PilotageFinancier.Application.Parsing;
using PilotageFinancier.Application.Services;
using PilotageFinancier.Forecasting;

namespace PilotageFinancier.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITabularParser, TabularParser>();
        services.AddScoped<ISsaForecaster, SsaForecaster>();
        services.AddScoped<IngestionService>();
        services.AddScoped<NormalisationService>();
        services.AddScoped<AgregationService>();
        services.AddScoped<PrevisionService>();
        return services;
    }
}
