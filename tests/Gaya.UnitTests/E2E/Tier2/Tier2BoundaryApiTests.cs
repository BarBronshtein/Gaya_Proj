using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier2;

public class Tier2BoundaryApiTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task T2_R3_01_PostCalculate_UnknownOperationKey_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "non-existent-op",
                FieldA = "1",
                FieldB = "2"
            });
        });

        // Verify audit log captured 404
        var logs = await _harness.AuditLogRepository.GetRecentAsync(5);
        var audit = logs.FirstOrDefault(l => l.StatusCode == 404);
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task T2_R3_02_PostCalculate_InactiveOperationKey_ThrowsInvalidOperationException()
    {
        // Arrange: Deactivate operation
        await _harness.OperationRepository.SetActiveStatusAsync("power", false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "power",
                FieldA = "2",
                FieldB = "3"
            });
        });

        // Verify audit log captured 400
        var logs = await _harness.AuditLogRepository.GetRecentAsync(5);
        var audit = logs.FirstOrDefault(l => l.StatusCode == 400);
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task T2_R3_03_PostOperations_DuplicateKey_UpsertsDefinitionSafely()
    {
        // Arrange: Initial create
        await _harness.CreateOperationAsync(new CreateOperationDto
        {
            Key = "discount-calc",
            DisplayName = "Discount V1",
            Category = "Arithmetic",
            RuleTemplate = "A * 0.9",
            FieldAPrompt = "Price",
            FieldBPrompt = "N/A"
        });

        // Act: Upsert with modified display name and template
        var updated = await _harness.CreateOperationAsync(new CreateOperationDto
        {
            Key = "discount-calc",
            DisplayName = "Discount V2 (20% Off)",
            Category = "Arithmetic",
            RuleTemplate = "A * 0.8",
            FieldAPrompt = "Price",
            FieldBPrompt = "N/A"
        });

        // Assert: Safely updated without duplicate key violation
        Assert.Equal("discount-calc", updated.Key);
        Assert.Equal("Discount V2 (20% Off)", updated.DisplayName);
    }

    [Fact]
    public async Task T2_R3_04_AuditLog_CapturesErrorStatusCodeOnFailure()
    {
        // Act: Cause 500 error via divide-by-zero
        try
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = "50",
                FieldB = "0"
            });
        }
        catch (DivideByZeroException)
        {
            // Expected
        }

        // Assert: Audit log captures status 500
        var logs = await _harness.AuditLogRepository.GetRecentAsync(5);
        var errAudit = logs.FirstOrDefault(l => l.StatusCode == 500);
        Assert.NotNull(errAudit);
        Assert.Contains("divide", errAudit.RequestBody);
    }

    [Fact]
    public async Task T2_R3_05_DeactivateOperation_ReflectsInGetOperations()
    {
        // Arrange
        await _harness.OperationRepository.SetActiveStatusAsync("modulo", false);

        // Act
        var allOps = await _harness.GetOperationsAsync();
        var moduloOp = allOps.FirstOrDefault(o => o.Key == "modulo");

        // Assert
        Assert.NotNull(moduloOp);
        Assert.False(moduloOp.IsActive);
    }
}
