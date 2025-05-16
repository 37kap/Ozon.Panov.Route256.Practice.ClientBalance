using Npgsql;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement;
using System.Data.Common;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.QueryExecution;

internal class QueryExecutor(INpgsqlConnectionFactory connectionFactory) : IQueryExecutor
{
    public async Task<IReadOnlyCollection<T>> ExecuteReaderAsync<T>(
        string query,
        Dictionary<string, object> param,
        Func<DbDataReader, CancellationToken, Task<T>> buildModel,
        CancellationToken token)
    {
        await using var connection = connectionFactory.GetConnection();
        await using var command = await BuildCommand(connection, query, param);
        await connection.OpenAsync(token);
        await using var reader = await command.ExecuteReaderAsync(token);

        var results = new List<T>();
        while (await reader.ReadAsync(token))
        {
            var result = await buildModel(reader, token);
            results.Add(result);
        }

        return results;
    }

    public async Task<T> ExecuteReaderSingleAsync<T>(
        string query,
        Dictionary<string, object> parameters,
        Func<DbDataReader, CancellationToken, Task<T>> buildModel,
        CancellationToken token)
    {
        await using var connection = connectionFactory.GetConnection();
        await using var command = await BuildCommand(connection, query, parameters);
        await connection.OpenAsync(token);
        await using var reader = await command.ExecuteReaderAsync(token);

        T? result = default;
        bool hasResult = false;

        while (await reader.ReadAsync(token))
        {
            if (hasResult)
            {
                throw new InvalidOperationException("The query returned more than one result.");
            }
            result = await buildModel(reader, token);
            hasResult = true;
        }

        if (!hasResult)
        {
            throw new NoQueryResultsException();
        }

        return result!;
    }

    public async Task ExecuteNonQueryAsync(
        string query, Dictionary<string, object> parameters, CancellationToken token)
    {
        await using var connection = connectionFactory.GetConnection();
        await using var command = await BuildCommand(connection, query, parameters);
        await connection.OpenAsync(token);
        await command.ExecuteNonQueryAsync(token);
    }

    private static async Task<NpgsqlCommand> BuildCommand(
        NpgsqlConnection connection, string query, Dictionary<string, object> parameters)
    {
        NpgsqlCommand? command = null;
        try
        {
            command = new NpgsqlCommand(query, connection)
            {
                CommandTimeout = 10
            };

            foreach ((string? name, object? value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            return command;
        }
        catch
        {
            if (command != null)
            {
                await command.DisposeAsync();
            }
            throw;
        }
    }
}