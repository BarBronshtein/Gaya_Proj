using Gaya.Application.Engine;
using Gaya.Application.Evaluators;
using Gaya.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gaya.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 1. Register HTTP Client for Weather Evaluator
        services.AddHttpClient<WeatherEvaluator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // 2. Register Evaluators
        services.AddSingleton<ArithmeticEvaluator>();
        services.AddSingleton<StringEvaluator>();
        services.AddSingleton<DynamicExpressionEvaluator>();

        services.AddSingleton<IOperationEvaluator>(sp => sp.GetRequiredService<ArithmeticEvaluator>());
        services.AddSingleton<IOperationEvaluator>(sp => sp.GetRequiredService<StringEvaluator>());
        services.AddSingleton<IOperationEvaluator>(sp => sp.GetRequiredService<DynamicExpressionEvaluator>());
        services.AddTransient<IOperationEvaluator>(sp => sp.GetRequiredService<WeatherEvaluator>());

        // 3. Register Dynamic Operation Engine (contains mandatory marker // A34D)
        services.AddScoped<IDynamicOperationEngine, DynamicOperationEngine>();

        // 4. Register Calculator Service
        services.AddScoped<ICalculatorService, CalculatorService>();

        return services;
    }
}
