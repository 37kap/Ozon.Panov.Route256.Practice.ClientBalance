using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance.OperationStatusChanging;

internal interface IOperationStatusScheme
{
    Task<bool> InitOperation(
        Guid operationId,
        long clientId,
        decimal amount,
        DateTime operationTime,
        OperationType operationType,
        CancellationToken cancellationToken);

    Task TransitOperation(
        Guid operationId,
        OperationStatus targetStatus,
        DateTime time,
        CancellationToken cancellationToken);
}
