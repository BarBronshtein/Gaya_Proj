using Gaya.Application.DTOs;

namespace Gaya.Application.Services;

public interface ICalculatorService
{
    Task<CalculationResponseDto> CalculateAsync(CalculationRequestDto request, CancellationToken ct = default);

    Task<IEnumerable<OperationDto>> GetOperationsAsync(bool activeOnly = false, CancellationToken ct = default);

    Task<OperationDto?> GetOperationByKeyAsync(string key, CancellationToken ct = default);

    Task<OperationDto> CreateOperationAsync(CreateOperationDto dto, CancellationToken ct = default);

    Task<OperationMetricsDto> GetMetricsAsync(string operationKey, CancellationToken ct = default);

    Task<IEnumerable<OperationHistoryDto>> GetHistoryAsync(int limit = 50, CancellationToken ct = default);

    Task SetOperationActiveStatusAsync(string key, bool isActive, CancellationToken ct = default);
}
