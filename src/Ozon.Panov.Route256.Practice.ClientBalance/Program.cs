using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement;
using Ozon.Panov.Route256.Practice.ClientBalance.Presentation.Grpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var config = builder.Configuration
            .AddEnvironmentVariables("ROUTE256_")
            .Build();

        builder.Services
            .AddApplication()
            .AddInfrastructure(config)
            .AddPresentation();

        var app = builder.Build();

        app.MigrateDatabase();
        app.MapGrpcService<ClientBalanceGrpcService>();
        app.MapGrpcReflectionService();

        app.Run();
    }
}
