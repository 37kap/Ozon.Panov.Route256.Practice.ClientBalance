using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure;

internal static class GrpcEntitiesMapper
{
    public static decimal ToDecimal(this Money money)
    {
        decimal units = money.Units;

        decimal nanos = money.Nanos / 1_000_000_000m;

        return units + nanos;
    }

    public static Money ToMoney(this decimal amount)
    {
        long units = (long)Math.Truncate(amount);

        int nanos = (int)((amount - units) * 1_000_000_000m);

        return new Money
        {
            Units = units,
            Nanos = nanos
        };
    }

    public static Application.ClientBalance.OperationStatusChanging.ChangeOperationStatus ToDto(this ChangeOperationStatus operationStatus)
    {
        return operationStatus switch
        {
            ChangeOperationStatus.Cancelled => Application.ClientBalance.OperationStatusChanging.ChangeOperationStatus.Cancelled,
            ChangeOperationStatus.Completed => Application.ClientBalance.OperationStatusChanging.ChangeOperationStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(operationStatus), operationStatus, null)
        };
    }

    public static OperationType ToGrpc(this Domain.BalanceOperations.OperationType dtoOperationType)
    {
        return dtoOperationType switch
        {
            Domain.BalanceOperations.OperationType.TopUp => OperationType.TopUp,
            Domain.BalanceOperations.OperationType.Withdraw => OperationType.Withdraw,
            _ => throw new ArgumentOutOfRangeException(nameof(dtoOperationType), dtoOperationType, null)
        };
    }

    public static OperationStatus ToGrpc(this Domain.BalanceOperations.OperationStatus operationStatus)
    {
        return operationStatus switch
        {
            Domain.BalanceOperations.OperationStatus.Pending => OperationStatus.Pending,
            Domain.BalanceOperations.OperationStatus.Cancelled => OperationStatus.Cancelled,
            Domain.BalanceOperations.OperationStatus.Completed => OperationStatus.Completed,
            Domain.BalanceOperations.OperationStatus.Reject => OperationStatus.Reject,
            _ => throw new ArgumentOutOfRangeException(nameof(operationStatus), operationStatus, null)
        };
    }
}
