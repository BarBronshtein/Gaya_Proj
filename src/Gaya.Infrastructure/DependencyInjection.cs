using Gaya.Domain.Repositories;
using Gaya.Infrastructure.Data;
using Gaya.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaya.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration != null)
        {
            services.AddSingleton(configuration);
        }

        services.AddSingleton<IDbConnectionFactory>(sp =>
        {
            var config = configuration ?? sp.GetService<IConfiguration>();
            if (config != null)
            {
                return new DbConnectionFactory(config);
            }

            return new DbConnectionFactory(new ConfigurationBuilder().Build());
        });

        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IOperationHistoryRepository, OperationHistoryRepository>();
        services.AddScoped<IApiAuditLogRepository, ApiAuditLogRepository>();
        services.AddScoped<ISystemErrorRepository, SystemErrorRepository>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services.AddInfrastructure(null);
    }
}
