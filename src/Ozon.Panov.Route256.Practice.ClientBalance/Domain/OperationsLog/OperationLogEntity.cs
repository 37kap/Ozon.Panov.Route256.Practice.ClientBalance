using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;

internal record OperationLogEntity(
    Guid OperationId,
    OperationType OperationType,
    long ClientId,
    decimal Amount,
    OperationStatus Status,
    DateTime Time);
