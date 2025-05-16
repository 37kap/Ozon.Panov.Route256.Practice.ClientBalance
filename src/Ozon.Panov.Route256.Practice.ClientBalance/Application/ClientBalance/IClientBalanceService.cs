using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance.OperationStatusChanging;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;

internal interface IClientBalanceService
{
    Task CreateClient(
        long clientId,
        CancellationToken cancellationToken);

    Task TopUpClientBalance(
        Guid operationId,
        long clientId,
        decimal amount,
        DateTime operationTime,
        CancellationToken cancellationToken);

    Task<bool> WithdrawClientBalance(
        Guid operationId,
        long clientId,
        decimal amount,
        DateTime operationTime,
        CancellationToken cancellationToken);

    Task ChangeOperationStatus(
        Guid operationId,
        ChangeOperationStatus operationStatus,
        DateTime time,
        CancellationToken cancellationToken);

    Task<decimal> QueryClientBalance(
        long clientId,
        CancellationToken cancellationToken);

    Task RemoveOutdatedOperations(
        IEnumerable<Guid> operations,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OperationsHistoryEntry>> QueryOperationsHistory(
        long clientId,
        int limit,
        int offset,
        CancellationToken cancellationToken);
}
