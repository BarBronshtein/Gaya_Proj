using Gaya.Domain.Entities;
using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier2;

public class Tier2BoundaryPersistenceTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task T2_R2_01_GetMonthlyCount_BoundaryTimestamps_StrictUtcCutoff()
    {
        // Arrange
        var currentMonthStart = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var justBeforeCutoff = currentMonthStart.AddMilliseconds(-1); // 2026-08-31 23:59:59.999Z
        var exactlyAtCutoff = currentMonthStart;                      // 2026-09-01 00:00:00.000Z

        // Record 1: Prior month
        await _harness.HistoryRepository.AddAsync(new OperationHistory
        {
            OperationKey = "add",
            FieldA = "1",
            FieldB = "1",
            Result = "2",
            DurationMs = 1,
            ExecutedAt = justBeforeCutoff
        });

        // Record 2: Current month exactly at 00:00:00.000Z
        await _harness.HistoryRepository.AddAsync(new OperationHistory
        {
            OperationKey = "add",
            FieldA = "2",
            FieldB = "2",
            Result = "4",
            DurationMs = 1,
            ExecutedAt = exactlyAtCutoff
        });

        // Act
        var count = await _harness.HistoryRepository.GetMonthlyCountAsync("add", currentMonthStart);

        // Assert: Exactly 1 record included in September 2026
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task T2_R2_02_GetRecent_FewerThan3Records_ReturnsActualCountWithoutError()
    {
        // Arrange: Only 1 execution
        await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "modulo",
            FieldA = "10",
            FieldB = "3"
        });

        // Act
        var recents = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("modulo", 3);

        // Assert: 1 record, no index out of bounds
        Assert.Single(recents);
        Assert.Equal("1", recents[0].Result);
    }

    [Fact]
    public async Task T2_R2_03_GetRecent_ZeroRecords_ReturnsEmptyList()
    {
        // Act: Query operation with no executions
        var recents = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("unused-op", 3);

        // Assert: Returns empty non-null list
        Assert.NotNull(recents);
        Assert.Empty(recents);
    }

    [Fact]
    public async Task T2_R2_04_GetRecent_KeyIsolation_DoesNotLeakAcrossOperations()
    {
        // Arrange: 3 records for add, 1 record for multiply
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "1", FieldB = "1" });
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "2", FieldB = "2" });
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "3", FieldB = "3" });

        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "multiply", FieldA = "5", FieldB = "5" });

        // Act
        var addRecents = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("add", 3);
        var mulRecents = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("multiply", 3);

        // Assert: Complete isolation
        Assert.Equal(3, addRecents.Count);
        Assert.Single(mulRecents);
        Assert.DoesNotContain(addRecents, r => r.OperationKey == "multiply");
        Assert.DoesNotContain(mulRecents, r => r.OperationKey == "add");
    }

    [Fact]
    public async Task T2_R2_05_AddApiAuditLog_LargePayload_PersistsWithoutTruncation()
    {
        // Arrange: 64KB JSON payload
        var largeString = new string('A', 64 * 1024);
        var log = new ApiAuditLog
        {
            Path = "/api/calculate",
            Method = "POST",
            StatusCode = 200,
            LatencyMs = 15,
            RequestBody = $"{{\"operationKey\":\"concat\",\"fieldA\":\"{largeString}\",\"fieldB\":\"suffix\"}}",
            ResponseBody = "{\"result\":\"ok\"}",
            ClientIp = "127.0.0.1"
        };

        // Act
        var id = await _harness.AuditLogRepository.AddAsync(log);

        // Assert
        Assert.True(id > 0);
        var recent = await _harness.AuditLogRepository.GetRecentAsync(1);
        Assert.NotNull(recent[0].RequestBody);
        Assert.Contains(largeString, recent[0].RequestBody);
    }

    [Fact]
    public async Task T2_R2_06_Persistence_SpecialCharactersAndHebrew_PersistsIntact()
    {
        // Arrange: Mixed Hebrew and special characters
        const string hebrewInputA = "שלום עולם! @#$%^&*()_+";
        const string hebrewInputB = "בדיקת מערכת Gaya";

        var response = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "concat",
            FieldA = hebrewInputA,
            FieldB = hebrewInputB
        });

        // Assert: Stored accurately
        Assert.Equal($"{hebrewInputA}{hebrewInputB}", response.Result);

        var records = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("concat", 1);
        Assert.Single(records);
        Assert.Equal(hebrewInputA, records[0].FieldA);
        Assert.Equal(hebrewInputB, records[0].FieldB);
    }
}
