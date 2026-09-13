using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Xunit;

namespace Gaya.UnitTests;

public class DomainModelTests
{
    [Fact]
    public void OperationDefinition_ShouldInitializeWithDefaults()
    {
        // Act
        var op = new OperationDefinition
        {
            Key = "test-op",
            DisplayName = "Test Operation",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A + B",
            FieldAPrompt = "Operand A",
            FieldBPrompt = "Operand B"
        };

        // Assert
        Assert.Equal("test-op", op.Key);
        Assert.Equal("Test Operation", op.DisplayName);
        Assert.Equal(OperationCategory.Arithmetic, op.Category);
        Assert.Equal("A + B", op.RuleTemplate);
        Assert.Equal("Operand A", op.FieldAPrompt);
        Assert.Equal("Operand B", op.FieldBPrompt);
        Assert.True(op.IsActive);
        Assert.True(op.CreatedAt <= DateTime.UtcNow);
        Assert.Null(op.UpdatedAt);
    }

    [Theory]
    [InlineData(OperationCategory.Arithmetic)]
    [InlineData(OperationCategory.String)]
    [InlineData(OperationCategory.ExternalApi)]
    public void OperationCategory_ShouldSupportAllDefinedCategories(OperationCategory category)
    {
        Assert.True(Enum.IsDefined(typeof(OperationCategory), category));
    }

    [Fact]
    public void OperationHistory_ShouldStoreExecutionDetailsCorrectly()
    {
        // Arrange
        var executedTime = DateTime.UtcNow;
        var history = new OperationHistory
        {
            Id = 42,
            OperationKey = "add",
            FieldA = "10.5",
            FieldB = "20.5",
            Result = "31",
            DurationMs = 12,
            ExecutedAt = executedTime
        };

        // Assert
        Assert.Equal(42, history.Id);
        Assert.Equal("add", history.OperationKey);
        Assert.Equal("10.5", history.FieldA);
        Assert.Equal("20.5", history.FieldB);
        Assert.Equal("31", history.Result);
        Assert.Equal(12, history.DurationMs);
        Assert.Equal(executedTime, history.ExecutedAt);
    }

    [Fact]
    public void ApiAuditLog_ShouldCaptureHttpTraceCorrectly()
    {
        // Arrange
        var audit = new ApiAuditLog
        {
            Id = 101,
            Path = "/api/calculate",
            Method = "POST",
            StatusCode = 200,
            LatencyMs = 45,
            RequestBody = "{\"operationKey\":\"add\",\"fieldA\":\"5\",\"fieldB\":\"5\"}",
            ResponseBody = "{\"result\":\"10\"}",
            ClientIp = "127.0.0.1"
        };

        // Assert
        Assert.Equal(101, audit.Id);
        Assert.Equal("/api/calculate", audit.Path);
        Assert.Equal("POST", audit.Method);
        Assert.Equal(200, audit.StatusCode);
        Assert.Equal(45, audit.LatencyMs);
        Assert.NotNull(audit.RequestBody);
        Assert.NotNull(audit.ResponseBody);
        Assert.Equal("127.0.0.1", audit.ClientIp);
        Assert.True(audit.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void SystemError_ShouldCaptureFaultMetadataCorrectly()
    {
        // Arrange
        var err = new SystemError
        {
            Id = 999,
            ErrorMessage = "Database connection timeout",
            StackTrace = "at Gaya.Infrastructure.Data.DatabaseInitializer...",
            SourcePath = "/api/operations",
            StatusCode = 500
        };

        // Assert
        Assert.Equal(999, err.Id);
        Assert.Equal("Database connection timeout", err.ErrorMessage);
        Assert.Contains("DatabaseInitializer", err.StackTrace);
        Assert.Equal("/api/operations", err.SourcePath);
        Assert.Equal(500, err.StatusCode);
        Assert.True(err.CreatedAt <= DateTime.UtcNow);
    }
}
