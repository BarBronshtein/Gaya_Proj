using Gaya.Domain.Repositories;
using Gaya.Infrastructure;
using Gaya.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gaya.UnitTests;

public class InfrastructureTests
{
    [Fact]
    public void DbConnectionFactory_ShouldParseMasterAndTargetConnectionString()
    {
        // Arrange
        const string connStr = "Server=localhost,1433;Database=OperationsDb;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=True;Encrypt=True;";

        // Act
        var factory = new DbConnectionFactory(connStr);

        // Assert
        Assert.Equal(connStr, factory.ConnectionString);
        Assert.Contains("OperationsDb", factory.ConnectionString);
        Assert.Contains("master", factory.MasterConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Database=OperationsDb", factory.MasterConnectionString);
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterAllRepositoriesAndInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost,1433;Database=OperationsDb;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=True;Encrypt=True;"
            })
            .Build();

        services.AddLogging();

        // Act
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IDbConnectionFactory>());
        Assert.NotNull(provider.GetService<IDatabaseInitializer>());
        Assert.NotNull(provider.GetService<IOperationRepository>());
        Assert.NotNull(provider.GetService<IOperationHistoryRepository>());
        Assert.NotNull(provider.GetService<IApiAuditLogRepository>());
        Assert.NotNull(provider.GetService<ISystemErrorRepository>());
    }

    [Fact]
    public void DbConnectionFactory_ShouldCreateValidDbConnectionInstance()
    {
        // Arrange
        const string connStr = "Server=localhost,1433;Database=OperationsDb;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=True;Encrypt=True;";
        var factory = new DbConnectionFactory(connStr);

        // Act
        using var conn = factory.CreateConnection();

        // Assert
        Assert.NotNull(conn);
        Assert.Equal("OperationsDb", conn.Database);
    }
}
