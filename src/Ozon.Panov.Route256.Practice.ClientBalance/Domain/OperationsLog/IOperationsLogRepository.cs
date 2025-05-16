using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;

internal interface IOperationsLogRepository
{
    Task Insert(
        OperationLogEntity operationLogEntity,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OperationsHistoryEntry>> Query(
        long clientId,
        int limit,
        int offset,
        CancellationToken cancellationToken);
}