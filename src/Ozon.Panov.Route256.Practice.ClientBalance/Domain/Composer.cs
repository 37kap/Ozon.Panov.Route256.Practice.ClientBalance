using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain;

internal static class Composer
{
    public static OperationLogEntity ToLogEntity(this OperationEntity operationEntity)
        => new(
            Amount: operationEntity.Amount,
            ClientId: operationEntity.ClientId,
            OperationId: operationEntity.OperationId,
            OperationType: operationEntity.OperationType,
            Status: operationEntity.Status,
            Time: operationEntity.Time
        );
}
