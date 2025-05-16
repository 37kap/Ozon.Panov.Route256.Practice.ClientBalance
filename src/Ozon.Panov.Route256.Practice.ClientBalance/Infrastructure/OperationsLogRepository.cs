using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.QueryExecution;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure;

internal sealed class OperationsLogRepository(
    IQueryExecutor queryExecutor) : IOperationsLogRepository
{
    public async Task Insert(OperationLogEntity operationLogEntity, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                insert into operations_log (
                                    operation_id, operation_type, client_id, amount, status, time)
                                values (@OperationId, @OperationType, @ClientId, @Amount, @Status, @Time);
                                """;

        var param = new Dictionary<string, object>
        {
            ["OperationId"] = operationLogEntity.OperationId,
            ["OperationType"] = operationLogEntity.OperationType,
            ["ClientId"] = operationLogEntity.ClientId,
            ["Amount"] = operationLogEntity.Amount,
            ["Status"] = operationLogEntity.Status,
            ["Time"] = operationLogEntity.Time
        };

        await queryExecutor.ExecuteNonQueryAsync(sqlQuery, param, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OperationsHistoryEntry>> Query(
        long clientId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var sqlQuery = """
                   WITH FinalStatuses AS (
                       SELECT
                           operation_id,
                           operation_type,
                           amount,
                           status,
                           time,
                           ROW_NUMBER() OVER (PARTITION BY operation_id ORDER BY time DESC) AS rn
                       FROM operations_log
                       WHERE client_id = @ClientId
                   )
                   SELECT
                       operation_id,
                       operation_type,
                       amount,
                       status,
                       time,
                       COUNT(*) OVER () AS total_count
                   FROM FinalStatuses
                   WHERE rn = 1
                   ORDER BY time
                   """;


        var param = new Dictionary<string, object>
        {
            ["ClientId"] = clientId,
        };

        if (limit > 0)
        {
            sqlQuery += "\nlimit @Limit";
            param.Add("Limit", limit);
        }

        if (offset > 0)
        {
            sqlQuery += "\noffset @Offset";
            param.Add("Offset", offset);
        }

        return await queryExecutor.ExecuteReaderAsync(
            sqlQuery,
            param,
            async (reader, token) => new OperationsHistoryEntry(
                OperationId: await reader.GetFieldValueAsync<Guid>(0, token),
                OperationType: await reader.GetFieldValueAsync<OperationType>(1, token),
                Amount: await reader.GetFieldValueAsync<decimal>(2, token),
                OperationStatus: await reader.GetFieldValueAsync<OperationStatus>(3, token),
                OperationTime: await reader.GetFieldValueAsync<DateTime>(4, token),
                TotalCount: await reader.GetFieldValueAsync<int>(5, token)),
            cancellationToken);
    }
}
