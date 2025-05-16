using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.AspNetCore;
using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance.OperationStatusChanging;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.OperationsLog;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.QueryExecution;
using Ozon.Panov.Route256.Practice.ClientBalance.Presentation.Grpc;
using System.Reflection;

namespace Ozon.Panov.Route256.Practice.ClientBalance;

internal static class Composer
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.Interceptors.Add<GrpcExceptionInterceptor>();
            options.Interceptors.Add<GrpcValidationInterceptor>();
        });

        services.AddGrpcReflection();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services
            .AddScoped<IOperationStatusScheme, OperationStatusScheme>()
            .AddScoped<IClientBalanceService, ClientBalanceService>()
            .AddValidation();
    }

    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("CLIENT_BALANCE_DB_CONNECTION_STRING")!;

        return services
            .AddMigration(connectionString)
            .AddSingleton<INpgsqlConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString))
            .AddSingleton<IQueryExecutor, QueryExecutor>()
            .AddRepositories();
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddScoped<IClientsBalanceRepository, ClientsBalanceRepository>()
            .AddScoped<IOperationsRepository, OperationsRepository>()
            .AddScoped<IOperationsLogRepository, OperationsLogRepository>();
    }

    private static IServiceCollection AddMigration(this IServiceCollection services,
        string connectionString)
    {
        return services.AddLogging(c => c.AddFluentMigratorConsole())
            .AddFluentMigratorCore()
            .ConfigureRunner(
                x => x.AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(Assembly.GetExecutingAssembly())
                    .For.Migrations());
    }

    private static IServiceCollection AddValidation(
        this IServiceCollection services)
    {
        return services
            .AddValidatorsFromAssemblyContaining<Program>()
            .AddFluentValidationAutoValidation();
    }
}
