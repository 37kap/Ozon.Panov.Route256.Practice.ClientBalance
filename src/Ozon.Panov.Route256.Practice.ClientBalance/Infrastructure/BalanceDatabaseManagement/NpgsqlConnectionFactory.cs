using Npgsql;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement;

internal sealed class NpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<OperationStatus>("operation_status");
        dataSourceBuilder.MapEnum<OperationType>("operation_type");
        _dataSource = dataSourceBuilder.Build();
    }

    public NpgsqlConnection GetConnection()
    {
        return _dataSource.CreateConnection();
    }
}