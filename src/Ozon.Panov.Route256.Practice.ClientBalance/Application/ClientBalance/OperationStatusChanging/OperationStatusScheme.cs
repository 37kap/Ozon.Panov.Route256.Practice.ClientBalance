using Ozon.Panov.Route256.Practice.ClientBalance.Domain;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance.OperationStatusChanging;

internal sealed class OperationStatusScheme(
    IClientsBalanceRepository clientsBalanceRepository,
    IOperationsRepository operationsRepository,
    IOperationsLogRepository operationsLogRepository,
    ILogger logger) : IOperationStatusScheme
{
    public async Task<bool> InitOperation(
        Guid operationId,
        long clientId,
        decimal amount,
        DateTime operationTime,
        OperationType operationType,
        CancellationToken cancellationToken)
    {
        var operation = new OperationEntity(
                OperationId: operationId,
                ClientId: clientId,
                Amount: amount,
                Status: GetInitialOperationStatus(operationType),
                OperationType: operationType,
                Time: operationTime);


        var action = GetOperationAction(operationType);
        var updateBalanceIsSuccess = await action(operation, cancellationToken);

        if (!updateBalanceIsSuccess)
        {
            operation = operation with
            {
                Status = OperationStatus.Reject,
            };
        }

        await operationsRepository.Insert(operation, cancellationToken);
        await operationsLogRepository.Insert(operation.ToLogEntity(), cancellationToken);

        return updateBalanceIsSuccess;
    }

    public async Task TransitOperation(
        Guid operationId,
        OperationStatus targetStatus,
        DateTime time,
        CancellationToken cancellationToken)
    {
        OperationEntity operation = await operationsRepository.Query(operationId, cancellationToken);

        var action = GetTransitionAction(operation, targetStatus);
        var updateBalanceIsSuccess = await action(operation, cancellationToken);

        if (updateBalanceIsSuccess)
        {
            var updatedOperation = operation with
            {
                Status = targetStatus,
                Time = time
            };

            await operationsRepository.Update(updatedOperation, cancellationToken);
            await operationsLogRepository.Insert(updatedOperation.ToLogEntity(), cancellationToken);
        }
    }

    private Func<OperationEntity, CancellationToken, Task<bool>> GetOperationAction
        (OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Withdraw
                => Withdraw,

            OperationType.TopUp
                => (_, _) => Task.FromResult(true),

            _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
        };
    }

    private Func<OperationEntity, CancellationToken, Task<bool>> GetTransitionAction
        (OperationEntity operation, OperationStatus targetStatus)
    {
        return (operation.Status, operation.OperationType, targetStatus) switch
        {
            (OperationStatus.Pending, OperationType.TopUp, OperationStatus.Completed) or
            (OperationStatus.Cancelled, OperationType.TopUp, OperationStatus.Completed) or
            (OperationStatus.Completed, OperationType.Withdraw, OperationStatus.Cancelled)
                => TopUp,

            (OperationStatus.Completed, OperationType.TopUp, OperationStatus.Cancelled) or
            (OperationStatus.Reject, OperationType.Withdraw, OperationStatus.Completed) or
            (OperationStatus.Cancelled, OperationType.Withdraw, OperationStatus.Completed)
                => Withdraw,

            (OperationStatus.Pending, OperationType.TopUp, OperationStatus.Cancelled)
                => (_, _) => Task.FromResult(true),

            _ => throw new InvalidOperationException($"Transition from {operation.Status} to {targetStatus} is invalid.")
        };
    }

    private async Task<bool> TopUp(OperationEntity operation, CancellationToken cancellationToken)
    {
        return await UpdateClientBalance(operation.ClientId, operation.Amount, cancellationToken);
    }

    private async Task<bool> Withdraw(OperationEntity operation, CancellationToken cancellationToken)
    {
        var currentBalance = await clientsBalanceRepository.Query(operation.ClientId, cancellationToken);

        if (currentBalance < operation.Amount)
        {
            return false;
        }

        return await UpdateClientBalance(operation.ClientId, -operation.Amount, cancellationToken);
    }

    private async Task<bool> UpdateClientBalance(long clientId, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            await clientsBalanceRepository.Update(clientId, amount, cancellationToken);
            return true;
        }
        catch (NoQueryResultsException e)
        {
            logger.LogError(e, "Client with client_id {ClientId} was not found in database", clientId);
            throw new NoQueryResultsException($"Client with client_id {clientId} was not found in database");
        }
    }

    private static OperationStatus GetInitialOperationStatus
        (OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Withdraw
                => OperationStatus.Completed,

            OperationType.TopUp
                => OperationStatus.Pending,

            _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
        };
    }
}