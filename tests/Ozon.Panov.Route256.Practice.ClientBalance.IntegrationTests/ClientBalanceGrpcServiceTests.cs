using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;
using Testcontainers.PostgreSql;
using OperationStatus = Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations.OperationStatus;

namespace Ozon.Panov.Route256.Practice.ClientBalance.IntegrationTests;

public class ClientBalanceGrpcServiceTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private GrpcChannel _channel;
    private ClientBalanceGrpc.ClientBalanceGrpcClient _client;
    private PostgreSqlContainer _postgresContainer;
    private string _connectionString;
    private INpgsqlConnectionFactory _connectionFactory;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("test_client_balance_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();

        _connectionString = _postgresContainer.GetConnectionString();

        Environment.SetEnvironmentVariable("ROUTE256_CLIENT_BALANCE_DB_CONNECTION_STRING", _connectionString);

        _factory = new WebApplicationFactory<Program>();
        var client = _factory.CreateDefaultClient();

        _channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = client
        });

        _client = new ClientBalanceGrpc.ClientBalanceGrpcClient(_channel);
        _connectionFactory = _factory.Services.GetRequiredService<INpgsqlConnectionFactory>();
    }

    public async Task DisposeAsync()
    {
        _channel.Dispose();
        await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task When_client_is_created_it_should_be_added_to_database()
    {
        // Arrange
        var request = new V1CreateClientRequest
        {
            ClientId = 12345
        };

        // Act
        _ = await _client.V1CreateClientAsync(request);

        // Assert
        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM clients WHERE client_id = @client_id",
            connection);
        command.Parameters.AddWithValue("client_id", request.ClientId);

        var count = (long)await command.ExecuteScalarAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task When_top_up_is_performed_it_should_be_recorded_in_database()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var operationId = Guid.NewGuid().ToString();
        var topUpRequest = new V1TopUpClientBalanceRequest
        {
            OperationId = operationId,
            ClientId = clientId,
            TopUpAmount = new Money { Units = 100, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };

        // Act
        _ = await _client.V1TopUpClientBalanceAsync(topUpRequest);

        // Assert
        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM operations WHERE operation_id = @operation_id",
            connection);
        command.Parameters.AddWithValue("operation_id", Guid.Parse(operationId));

        var count = (long)await command.ExecuteScalarAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task When_withdraw_is_requested_and_balance_is_sufficient_it_should_process_withdraw()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var topUpOperationId = Guid.NewGuid().ToString();
        var topUpRequest = new V1TopUpClientBalanceRequest
        {
            OperationId = topUpOperationId,
            ClientId = clientId,
            TopUpAmount = new Money { Units = 200, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1TopUpClientBalanceAsync(topUpRequest);

        var confirmTopUpRequest = new V1ChangeOperationStatusRequest
        {
            OperationId = topUpOperationId,
            OperationType = OperationType.TopUp,
            ClientId = clientId,
            OperationStatus = ChangeOperationStatus.Completed,
            ChangeTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1ChangeOperationStatusAsync(confirmTopUpRequest);

        var withdrawOperationId = Guid.NewGuid().ToString();
        var withdrawRequest = new V1WithdrawClientBalanceRequest
        {
            OperationId = withdrawOperationId,
            ClientId = clientId,
            WithdrawAmount = new Money { Units = 150, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };

        // Act
        var withdrawResponse = await _client.V1WithdrawClientBalanceAsync(withdrawRequest);

        // Assert
        Assert.True(withdrawResponse.WithdrawPossible);

        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync();

        var balanceCommand =
            new NpgsqlCommand("SELECT balance FROM clients WHERE client_id = @client_id", connection);
        balanceCommand.Parameters.AddWithValue("client_id", clientId);

        var balance = (decimal)await balanceCommand.ExecuteScalarAsync();

        Assert.Equal(50m, balance); // 200 - 150 = 50
    }

    [Fact]
    public async Task When_withdraw_is_requested_and_balance_is_insufficient_it_should_reject_withdraw()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var withdrawOperationId = Guid.NewGuid().ToString();
        var withdrawRequest = new V1WithdrawClientBalanceRequest
        {
            OperationId = withdrawOperationId,
            ClientId = clientId,
            WithdrawAmount = new Money { Units = 150, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };

        // Act
        var withdrawResponse = await _client.V1WithdrawClientBalanceAsync(withdrawRequest);

        // Assert
        Assert.False(withdrawResponse.WithdrawPossible);

        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync();

        var balanceCommand =
            new NpgsqlCommand("SELECT balance FROM clients WHERE client_id = @client_id", connection);
        balanceCommand.Parameters.AddWithValue("client_id", clientId);

        var balanceResult = await balanceCommand.ExecuteScalarAsync();

        Assert.Equal(0m, (decimal)balanceResult);
    }

    [Fact]
    public async Task When_operation_status_is_changed_it_should_update_status_in_database()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var topUpOperationId = Guid.NewGuid().ToString();
        var topUpRequest = new V1TopUpClientBalanceRequest
        {
            OperationId = topUpOperationId,
            ClientId = clientId,
            TopUpAmount = new Money { Units = 100, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1TopUpClientBalanceAsync(topUpRequest);

        // Act
        var changeStatusRequest = new V1ChangeOperationStatusRequest
        {
            OperationId = topUpOperationId,
            OperationType = OperationType.TopUp,
            ClientId = clientId,
            OperationStatus = ChangeOperationStatus.Completed,
            ChangeTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1ChangeOperationStatusAsync(changeStatusRequest);

        // Assert
        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync();

        var statusCommand = new NpgsqlCommand(
            "SELECT status FROM operations WHERE operation_id = @operation_id",
            connection);
        statusCommand.Parameters.AddWithValue("operation_id", Guid.Parse(topUpOperationId));

        var statusResult = (OperationStatus)await statusCommand.ExecuteScalarAsync();

        Assert.Equal(OperationStatus.Completed, statusResult);
    }

    [Fact]
    public async Task When_client_balance_is_queried_it_should_return_correct_balance()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var topUpOperationId = Guid.NewGuid().ToString();
        var topUpRequest = new V1TopUpClientBalanceRequest
        {
            OperationId = topUpOperationId,
            ClientId = clientId,
            TopUpAmount = new Money { Units = 100, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1TopUpClientBalanceAsync(topUpRequest);

        var confirmTopUpRequest = new V1ChangeOperationStatusRequest
        {
            OperationId = topUpOperationId,
            OperationType = OperationType.TopUp,
            ClientId = clientId,
            OperationStatus = ChangeOperationStatus.Completed,
            ChangeTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1ChangeOperationStatusAsync(confirmTopUpRequest);

        // Act
        var balanceResponse =
            await _client.V1QueryClientBalanceAsync(new V1QueryClientBalanceRequest { ClientId = clientId });

        // Assert
        Assert.Equal(100m, balanceResponse.Balance.Units);
    }

    [Fact]
    public async Task When_outdated_operations_are_removed_they_should_be_deleted_from_database()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var topUpOperationId = Guid.NewGuid().ToString();
        var topUpRequest = new V1TopUpClientBalanceRequest
        {
            OperationId = topUpOperationId,
            ClientId = clientId,
            TopUpAmount = new Money { Units = 100, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1TopUpClientBalanceAsync(topUpRequest);

        var withdrawOperationId = Guid.NewGuid().ToString();
        var withdrawRequest = new V1WithdrawClientBalanceRequest
        {
            OperationId = withdrawOperationId,
            ClientId = clientId,
            WithdrawAmount = new Money { Units = 50, Nanos = 0 },
            OperationTime = DateTime.UtcNow.ToTimestamp()
        };
        await _client.V1WithdrawClientBalanceAsync(withdrawRequest);

        // Act
        var removeOperations = new List<V1RemoveOutdatedOperationsRequest.Types.Operation>
        {
            new()
            {
                OperationId = topUpOperationId,
                OperationType = OperationType.TopUp
            },
            new()
            {
                OperationId = withdrawOperationId,
                OperationType = OperationType.Withdraw
            }
        };

        await _client.V1RemoveOutdatedOperationsAsync(new V1RemoveOutdatedOperationsRequest
        {
            Operations =
            {
                removeOperations
            }
        });

        // Assert
        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync();

        var topUpCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM operations WHERE operation_id = @operation_id AND operation_type = @operation_type",
            connection);
        topUpCommand.Parameters.AddWithValue("operation_id", Guid.Parse(topUpOperationId));
        topUpCommand.Parameters.AddWithValue("operation_type", Domain.BalanceOperations.OperationType.TopUp);

        var topUpCount = (long)await topUpCommand.ExecuteScalarAsync();
        Assert.Equal(0, topUpCount);

        var withdrawCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM operations WHERE operation_id = @operation_id AND operation_type = @operation_type",
            connection);
        withdrawCommand.Parameters.AddWithValue("operation_id", Guid.Parse(withdrawOperationId));
        withdrawCommand.Parameters.AddWithValue("operation_type", Domain.BalanceOperations.OperationType.Withdraw);

        var withdrawCount = (long)await withdrawCommand.ExecuteScalarAsync();
        Assert.Equal(0, withdrawCount);
    }

    [Fact]
    public async Task When_operations_history_is_queried_it_should_return_correct_history()
    {
        // Arrange
        var clientId = 12345;
        var createClientRequest = new V1CreateClientRequest { ClientId = clientId };
        await _client.V1CreateClientAsync(createClientRequest);

        var topUpOperationId = Guid.NewGuid().ToString();
        var topUpAmount = new Money { Units = 100, Nanos = 0 };
        var topUpTime = DateTime.UtcNow.AddMinutes(-10);
        var topUpRequest = new V1TopUpClientBalanceRequest
        {
            OperationId = topUpOperationId,
            ClientId = clientId,
            TopUpAmount = topUpAmount,
            OperationTime = topUpTime.ToTimestamp()
        };
        await _client.V1TopUpClientBalanceAsync(topUpRequest);

        DateTime topUpStatusChangeTime = DateTime.UtcNow;
        var confirmTopUpRequest = new V1ChangeOperationStatusRequest
        {
            OperationId = topUpOperationId,
            OperationType = OperationType.TopUp,
            ClientId = clientId,
            OperationStatus = ChangeOperationStatus.Completed,
            ChangeTime = topUpStatusChangeTime.ToTimestamp()
        };
        await _client.V1ChangeOperationStatusAsync(confirmTopUpRequest);

        var withdrawOperationId = Guid.NewGuid().ToString();
        var withdrawAmount = new Money { Units = 50, Nanos = 0 };
        var withdrawTime = DateTime.UtcNow.AddMinutes(-5);
        var withdrawRequest = new V1WithdrawClientBalanceRequest
        {
            OperationId = withdrawOperationId,
            ClientId = clientId,
            WithdrawAmount = withdrawAmount,
            OperationTime = withdrawTime.ToTimestamp()
        };
        await _client.V1WithdrawClientBalanceAsync(withdrawRequest);

        DateTime withdrawStatusChangeTime = DateTime.UtcNow;
        var confirmWithdrawRequest = new V1ChangeOperationStatusRequest
        {
            OperationId = withdrawOperationId,
            OperationType = OperationType.Withdraw,
            ClientId = clientId,
            OperationStatus = ChangeOperationStatus.Cancelled,
            ChangeTime = withdrawStatusChangeTime.ToTimestamp()
        };
        await _client.V1ChangeOperationStatusAsync(confirmWithdrawRequest);

        // Act
        var operationsHistoryRequest = new V1QueryOperationsHistoryRequest
        {
            ClientId = clientId,
            Limit = 10,
            Offset = 0
        };

        var operations = new List<V1QueryOperationsHistoryResponse>();
        using var call = _client.V1QueryOperationsHistory(operationsHistoryRequest);
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            operations.Add(call.ResponseStream.Current);
        }

        // Assert
        Assert.Equal(2, operations.Count);

        var sortedOperations = operations.OrderBy(o => o.OperationTime.ToDateTime()).ToList();
        var operation1 = sortedOperations[0];

        Assert.Equal(topUpOperationId, operation1.OperationId);
        Assert.Equal(OperationType.TopUp, operation1.OperationType);
        Assert.Equal(topUpAmount.Units, operation1.Amount.Units);
        Assert.Equal(Proto.ClientBalanceGrpc.OperationStatus.Completed, operation1.OperationStatus);
        Assert.Equal(topUpStatusChangeTime, operation1.OperationTime.ToDateTime(), TimeSpan.FromSeconds(1));

        var operation2 = sortedOperations[1];

        Assert.Equal(withdrawOperationId, operation2.OperationId);
        Assert.Equal(OperationType.Withdraw, operation2.OperationType);
        Assert.Equal(withdrawAmount.Units, operation2.Amount.Units);
        Assert.Equal(Proto.ClientBalanceGrpc.OperationStatus.Cancelled, operation2.OperationStatus);
        Assert.Equal(withdrawStatusChangeTime, operation2.OperationTime.ToDateTime(), TimeSpan.FromSeconds(1));
    }
}