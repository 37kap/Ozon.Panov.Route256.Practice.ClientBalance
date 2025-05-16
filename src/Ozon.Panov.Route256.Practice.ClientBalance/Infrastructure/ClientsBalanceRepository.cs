using Npgsql;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.QueryExecution;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure;

internal sealed class ClientsBalanceRepository(
    IQueryExecutor queryExecutor,
    ILogger<ClientsBalanceRepository> logger) :
    IClientsBalanceRepository
{

    public async Task<decimal> Query(long clientId, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                select balance
                                from clients
                                where client_id = @ClientId
                                FOR UPDATE;
                                """;

        var param = new Dictionary<string, object>
        {
            ["ClientId"] = clientId
        };

        try
        {
            var result = await queryExecutor.ExecuteReaderSingleAsync<decimal>(
                sqlQuery,
                param,
                async (reader, token) => await reader.GetFieldValueAsync<decimal>(0, token),
                cancellationToken);

            return result;
        }
        catch (NoQueryResultsException e)
        {
            logger.LogError(e, "Client with client_id {ClientId} was not found in database", clientId);
            throw new NoQueryResultsException($"Client with client_id {clientId} was not found in database");
        }
    }

    public async Task Insert(
        long clientId,
        decimal balance,
        CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                insert into
                                clients (client_id, balance)
                                values (@ClientId, @Balance);
                                """;

        var parameters = new Dictionary<string, object>
        {
            ["ClientId"] = clientId,
            ["Balance"] = balance
        };

        try
        {
            await queryExecutor.ExecuteNonQueryAsync(sqlQuery, parameters, cancellationToken);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            logger.LogError(e, "Client with client_id {Id} already exists in database", clientId);
            throw new ClientAlreadyExistsException(clientId);
        }
    }

    public async Task Update(
        long clientId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                            UPDATE clients
                            SET balance = balance + @Amount
                            WHERE client_id = @ClientId;
                            """;

        var param = new Dictionary<string, object>
        {
            ["ClientId"] = clientId,
            ["Amount"] = amount
        };

        await queryExecutor.ExecuteNonQueryAsync(sqlQuery, param, cancellationToken);
    }
}