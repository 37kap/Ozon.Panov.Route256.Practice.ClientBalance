using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance.OperationStatusChanging;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;
using System.Transactions;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;

internal sealed class ClientBalanceService(
    IClientsBalanceRepository clientsBalanceRepository,
    IOperationsRepository operationsRepository,
    IOperationsLogRepository operationsLogRepository,
    IOperationStatusScheme statusScheme) : IClientBalanceService
{
    private const decimal DEFAULT_CLIENT_BALANCE = 0.0m;
    public async Task CreateClient(long clientId, CancellationToken cancellationToken)
    {
        await clientsBalanceRepository.Insert(
            clientId,
            balance: DEFAULT_CLIENT_BALANCE,
            cancellationToken);
    }

    public async Task TopUpClientBalance(
        Guid operationId,
        long clientId,
        decimal amount,
        DateTime operationTime,
        CancellationToken cancellationToken)
    {
        using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        await statusScheme.InitOperation(
                operationId: operationId,
                clientId: clientId,
                amount: amount,
                operationTime: operationTime,
                operationType: OperationType.TopUp,
                cancellationToken: cancellationToken);

        ts.Complete();
    }

    public async Task<bool> WithdrawClientBalance(
        Guid operationId,
        long clientId,
        decimal amount,
        DateTime operationTime,
        CancellationToken cancellationToken)
    {
        using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var withdrawCompleted =
            await statusScheme.InitOperation(
                operationId: operationId,
                clientId: clientId,
                amount: amount,
                operationTime: operationTime,
                operationType: OperationType.Withdraw,
                cancellationToken: cancellationToken);

        ts.Complete();

        return withdrawCompleted;
    }

    public async Task ChangeOperationStatus(
        Guid operationId,
        ChangeOperationStatus changeOperationStatus,
        DateTime time,
        CancellationToken cancellationToken)
    {
        using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var targetStatus = changeOperationStatus.ToOperationStatus();

        await statusScheme.TransitOperation(operationId, targetStatus, time, cancellationToken);

        ts.Complete();
    }

    public async Task<decimal> QueryClientBalance(long clientId, CancellationToken cancellationToken)
    {
        return await clientsBalanceRepository.Query(clientId, cancellationToken);

    }

    public async Task RemoveOutdatedOperations(
        IEnumerable<Guid> operations,
        CancellationToken cancellationToken)
    {
        using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        await operationsRepository.Delete(operations, cancellationToken);

        ts.Complete();
    }

    public async Task<IReadOnlyCollection<OperationsHistoryEntry>> QueryOperationsHistory(
        long clientId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var operationsHistory = await operationsLogRepository.Query(
            clientId,
            limit,
            offset,
            cancellationToken);

        return operationsHistory;
    }
}