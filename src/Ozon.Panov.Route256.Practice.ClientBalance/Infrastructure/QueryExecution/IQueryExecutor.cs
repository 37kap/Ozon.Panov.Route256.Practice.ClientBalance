using System.Data.Common;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.QueryExecution;

internal interface IQueryExecutor
{
    Task<IReadOnlyCollection<T>> ExecuteReaderAsync<T>(
        string query,
        Dictionary<string, object> param,
        Func<DbDataReader, CancellationToken, Task<T>> buildModel,
        CancellationToken token);

    Task<T> ExecuteReaderSingleAsync<T>(
        string query,
        Dictionary<string, object> parameters,
        Func<DbDataReader, CancellationToken, Task<T>> buildModel,
        CancellationToken token);

    Task ExecuteNonQueryAsync(
        string query, Dictionary<string, object> parameters, CancellationToken token);


}
