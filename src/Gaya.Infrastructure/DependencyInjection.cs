using Gaya.Domain.Repositories;
using Gaya.Infrastructure.Data;
using Gaya.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaya.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IOperationHistoryRepository, OperationHistoryRepository>();
        services.AddScoped<IApiAuditLogRepository, ApiAuditLogRepository>();
        services.AddScoped<ISystemErrorRepository, SystemErrorRepository>();

        return services;
    }
}
