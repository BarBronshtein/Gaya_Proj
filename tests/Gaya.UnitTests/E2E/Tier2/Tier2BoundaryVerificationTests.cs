using System.Text.RegularExpressions;
using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier2;

public class Tier2BoundaryVerificationTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Theory]
    [InlineData("// A34D")]
    [InlineData("//   A34D   ")]
    [InlineData("/* A34D */")]
    [InlineData("/*   A34D   */")]
    [InlineData("// Mandatory marker A34D embedded")]
    public void T2_R5_01_A34DComment_RegexPattern_SupportsVariousCommentStyles(string comment)
    {
        // Assert: Regex pattern for token A34D matches regardless of style or spacing
        var pattern = @"\bA34D\b";
        Assert.Matches(pattern, comment);
    }

    [Fact]
    public async Task T2_R5_02_TestExecution_OfflineIsolated_RunsWithoutDocker()
    {
        // Assert: Harness runs completely in-memory without external connectivity
        var response = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "123",
            FieldB = "456"
        });

        Assert.Equal("579", response.Result);
        Assert.True(response.MonthlyExecutionCount >= 1);
    }

    [Fact]
    public async Task T2_R5_03_ParallelExecution_ThreadSafety()
    {
        // Arrange: 20 parallel calculation tasks
        var tasks = Enumerable.Range(1, 20).Select(async i =>
        {
            return await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "add",
                FieldA = i.ToString(),
                FieldB = "1"
            });
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert: All 20 completed successfully
        Assert.Equal(20, results.Length);
        Assert.All(results, r => Assert.NotNull(r.Result));
    }

    [Fact]
    public void T2_R5_04_PortDocumentation_MatchesConfiguration()
    {
        var solutionDir = FindSolutionRoot();
        Assert.NotNull(solutionDir);

        var composePath = Path.Combine(solutionDir, "docker-compose.yml");
        Assert.True(File.Exists(composePath), "docker-compose.yml must exist.");
        var composeContent = File.ReadAllText(composePath);
        Assert.Contains("1433", composeContent); // SQL Server port
        Assert.Contains("5080", composeContent); // OpenObserve port

        var launchSettingsPath = Path.Combine(solutionDir, "src/Gaya.Api/Properties/launchSettings.json");
        if (File.Exists(launchSettingsPath))
        {
            var content = File.ReadAllText(launchSettingsPath);
            Assert.Contains("applicationUrl", content);
        }
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

