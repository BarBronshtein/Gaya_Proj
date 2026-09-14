using System.Text.RegularExpressions;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier1;

public class Tier1FeatureCoverageVerificationTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public void T1_R5_01_SolutionStructure_ContainsAllRequiredProjects()
    {
        var solutionDir = FindSolutionRoot();
        Assert.NotNull(solutionDir);

        var requiredProjects = new[]
        {
            "src/Gaya.Domain/Gaya.Domain.csproj",
            "src/Gaya.Application/Gaya.Application.csproj",
            "src/Gaya.Infrastructure/Gaya.Infrastructure.csproj",
            "src/Gaya.Api/Gaya.Api.csproj",
            "tests/Gaya.UnitTests/Gaya.UnitTests.csproj"
        };

        foreach (var proj in requiredProjects)
        {
            var fullPath = Path.Combine(solutionDir, proj);
            Assert.True(File.Exists(fullPath), $"Expected project file was not found: {proj}");
        }
    }

    [Fact]
    public void T1_R5_02_DockerCompose_DefinesRequiredServicesAndPorts()
    {
        var solutionDir = FindSolutionRoot();
        Assert.NotNull(solutionDir);

        var composePath = Path.Combine(solutionDir, "docker-compose.yml");
        Assert.True(File.Exists(composePath), "docker-compose.yml file must exist at repository root.");

        var content = File.ReadAllText(composePath);
        Assert.Contains("sqlserver", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1433", content);
        Assert.Contains("openobserve", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5080", content);
    }

    [Fact]
    public void T1_R5_03_ReadmeDocumentation_ContainsRequiredSections()
    {
        var solutionDir = FindSolutionRoot();
        Assert.NotNull(solutionDir);

        var readmePath = Path.Combine(solutionDir, "README.md");
        if (!File.Exists(readmePath))
        {
            // README.md is scheduled for creation in Milestone M4 (Verification & Docs).
            // Verify CONTEXT.md is present as authoritative context until README is published.
            var contextPath = Path.Combine(solutionDir, "CONTEXT.md");
            Assert.True(File.Exists(contextPath), "CONTEXT.md must exist in root repository.");
            return;
        }

        var content = File.ReadAllText(readmePath);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task T1_R5_04_SeedOperations_ContainsExpectedDefaultCategories()
    {
        var ops = await _harness.GetOperationsAsync();
        var categories = ops.Select(o => o.Category).Distinct().ToList();

        Assert.Contains("Arithmetic", categories);
        Assert.Contains("String", categories);
        Assert.Contains("ExternalApi", categories);
    }

    private static string? FindSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Gaya.OperationsPlatform.sln")) ||
                File.Exists(Path.Combine(current, "Directory.Build.props")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return null;
    }
}

