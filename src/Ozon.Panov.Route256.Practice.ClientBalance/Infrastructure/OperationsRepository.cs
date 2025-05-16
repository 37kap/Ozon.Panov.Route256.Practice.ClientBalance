using Npgsql;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.QueryExecution;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure;

internal sealed class OperationsRepository(
    IQueryExecutor queryExecutor,
    ILogger<OperationsRepository> logger) : IOperationsRepository
{
    public async Task Insert(OperationEntity operation, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                insert into
                                    operations (operation_id, client_id, amount, status, operation_type, time)
                                    values (@OperationId, @ClientId, @Amount, @Status, @OperationType, @Time);
                                """;

        var param = new Dictionary<string, object>
        {
            ["OperationId"] = operation.OperationId,
            ["ClientId"] = operation.ClientId,
            ["Amount"] = operation.Amount,
            ["Status"] = operation.Status,
            ["OperationType"] = operation.OperationType,
            ["Time"] = operation.Time
        };

        try
        {
            await queryExecutor.ExecuteNonQueryAsync(sqlQuery, param, cancellationToken);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            logger.LogError(e, "Operation with operation_id {Id} already exists in database", operation.OperationId);
            throw new OperationAlreadyExistsException(operation.OperationId);
        }
    }

    public async Task Update(OperationEntity operation, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                update operations
                                set amount = @Amount,
                                    status = @Status,
                                    time = @Time
                                where operation_id = @OperationId;
                                """;

        var param = new Dictionary<string, object>
        {
            ["OperationId"] = operation.OperationId,
            ["Amount"] = operation.Amount,
            ["Status"] = operation.Status,
            ["Time"] = operation.Time
        };

        await queryExecutor.ExecuteNonQueryAsync(sqlQuery, param, cancellationToken);
    }

    public async Task<OperationEntity> Query(Guid operationId, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                select operation_id, client_id, amount, status, operation_type, time
                                from operations
                                where operation_id = @OperationId;
                                """;

        var param = new Dictionary<string, object>
        {
            ["OperationId"] = operationId
        };

        return await queryExecutor.ExecuteReaderSingleAsync<OperationEntity>(
            sqlQuery,
            param,
            async (reader, token) => new OperationEntity(
                OperationId: await reader.GetFieldValueAsync<Guid>(0, token),
                ClientId: await reader.GetFieldValueAsync<long>(1, token),
                Amount: await reader.GetFieldValueAsync<decimal>(2, token),
                Status: await reader.GetFieldValueAsync<OperationStatus>(3, token),
                OperationType: await reader.GetFieldValueAsync<OperationType>(4, token),
                Time: await reader.GetFieldValueAsync<DateTime>(5, token)),
            cancellationToken);
    }

    public async Task Delete(IEnumerable<Guid>? operationIds, CancellationToken cancellationToken)
    {
        if (operationIds is null ||
            operationIds.Count() == 0)
        {
            return;
        }

        const string sqlQuery = """
                                delete from operations
                                where operation_id = any(@OperationIds);
                                """;

        var param = new Dictionary<string, object>
        {
            ["OperationIds"] = operationIds
        };

        await queryExecutor.ExecuteNonQueryAsync(sqlQuery, param, cancellationToken);
    }
}
