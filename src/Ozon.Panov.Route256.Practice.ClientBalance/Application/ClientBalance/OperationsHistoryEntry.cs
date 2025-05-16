using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;

internal record OperationsHistoryEntry(
    Guid OperationId,
    OperationType OperationType,
    decimal Amount,
    OperationStatus OperationStatus,
    DateTime OperationTime,
    int TotalCount);