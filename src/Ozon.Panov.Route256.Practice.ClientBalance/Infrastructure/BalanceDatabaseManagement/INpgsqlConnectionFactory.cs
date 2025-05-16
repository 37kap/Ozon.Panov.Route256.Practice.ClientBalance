using Npgsql;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement;

public interface INpgsqlConnectionFactory
{
    NpgsqlConnection GetConnection();
}