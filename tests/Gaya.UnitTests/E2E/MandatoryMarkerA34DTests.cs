using System.Text.RegularExpressions;
using Xunit;

namespace Gaya.UnitTests.E2E;

public class MandatoryMarkerA34DTests
{
    [Fact]
    public void Codebase_MustContainMandatoryA34DComment()
    {
        var solutionDir = FindSolutionRoot();
        Assert.NotNull(solutionDir);

        var srcDir = Path.Combine(solutionDir, "src");
        Assert.True(Directory.Exists(srcDir), "Directory 'src/' must exist.");

        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
        var matchingFiles = new List<string>();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            if (Regex.IsMatch(content, @"\bA34D\b"))
            {
                matchingFiles.Add(file);
            }
        }

        Assert.True(matchingFiles.Count > 0,
            "Mandatory code comment 'A34D' was not found in any C# file under 'src/'. " +
            "Per Requirement R1 and Section 2.1, comment '// A34D' must be embedded directly in the dynamic operations engine.");
    }

    [Theory]
    [InlineData("// A34D: Mandatory calculation engine token")]
    [InlineData("//   A34D  ")]
    [InlineData("/* A34D */")]
    public void RegexValidator_MatchesAllValidCommentStyles(string comment)
    {
        Assert.Matches(@"\bA34D\b", comment);
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
