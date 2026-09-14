using Gaya.Application;
using Gaya.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Gaya Application layer services (engine, evaluators, calculator service).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services.AddApplication();
    }

    /// <summary>
    /// Registers Gaya Infrastructure layer services (Dapper repositories, connection factory, DB initializer).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        return services.AddInfrastructure(configuration);
    }
}
