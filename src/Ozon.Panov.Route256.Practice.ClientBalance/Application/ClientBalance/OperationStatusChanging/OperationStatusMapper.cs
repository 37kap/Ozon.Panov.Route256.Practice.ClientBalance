using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance.OperationStatusChanging;

internal static class OperationStatusMapper
{
    public static OperationStatus ToOperationStatus(this ChangeOperationStatus operationStatus)
    {
        return operationStatus switch
        {
            ChangeOperationStatus.Cancelled => OperationStatus.Cancelled,
            ChangeOperationStatus.Completed => OperationStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(operationStatus), operationStatus, null)
        };
    }
}
