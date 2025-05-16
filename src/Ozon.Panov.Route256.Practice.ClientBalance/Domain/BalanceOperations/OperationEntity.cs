namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

internal record OperationEntity(
    Guid OperationId,
    long ClientId,
    decimal Amount,
    OperationStatus Status,
    OperationType OperationType,
    DateTime Time);