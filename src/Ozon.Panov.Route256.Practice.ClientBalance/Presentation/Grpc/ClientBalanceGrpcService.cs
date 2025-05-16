using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ozon.Panov.Route256.Practice.ClientBalance.Application.ClientBalance;
using Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Presentation.Grpc;

internal sealed class ClientBalanceGrpcService(IClientBalanceService clientBalanceService)
    : ClientBalanceGrpc.ClientBalanceGrpcBase
{
    public override async Task<V1CreateClientResponse> V1CreateClient(
        V1CreateClientRequest request,
        ServerCallContext context)
    {
        await clientBalanceService.CreateClient(request.ClientId, context.CancellationToken);

        return new V1CreateClientResponse();
    }

    public override async Task<V1TopUpClientBalanceResponse> V1TopUpClientBalance(
        V1TopUpClientBalanceRequest request,
        ServerCallContext context)
    {
        await clientBalanceService.TopUpClientBalance(
            Guid.Parse(request.OperationId),
            request.ClientId,
            request.TopUpAmount.ToDecimal(),
            request.OperationTime.ToDateTime(),
            context.CancellationToken);

        return new V1TopUpClientBalanceResponse();
    }

    public override async Task<V1WithdrawClientBalanceResponse> V1WithdrawClientBalance(
        V1WithdrawClientBalanceRequest request,
        ServerCallContext context)
    {
        bool withdrawPossible = await clientBalanceService.WithdrawClientBalance(
            Guid.Parse(request.OperationId),
            request.ClientId,
            request.WithdrawAmount.ToDecimal(),
            request.OperationTime.ToDateTime(),
            context.CancellationToken);

        return new V1WithdrawClientBalanceResponse
        {
            WithdrawPossible = withdrawPossible
        };
    }

    public override async Task<V1ChangeOperationStatusResponse> V1ChangeOperationStatus(
        V1ChangeOperationStatusRequest request,
        ServerCallContext context)
    {
        await clientBalanceService.ChangeOperationStatus(
            Guid.Parse(request.OperationId),
            request.OperationStatus.ToDto(),
            request.ChangeTime.ToDateTime(),
            context.CancellationToken);

        return new V1ChangeOperationStatusResponse();
    }

    public override async Task<V1QueryClientBalanceResponse> V1QueryClientBalance(
        V1QueryClientBalanceRequest request,
        ServerCallContext context)
    {
        decimal balance = await clientBalanceService.QueryClientBalance(request.ClientId, context.CancellationToken);

        return new V1QueryClientBalanceResponse
        {
            Balance = balance.ToMoney()
        };
    }

    public override async Task<V1RemoveOutdatedOperationsResponse> V1RemoveOutdatedOperations(
        V1RemoveOutdatedOperationsRequest request,
        ServerCallContext context)
    {
        var operations = request.Operations
            .Select(operation => Guid.Parse(operation.OperationId))
            .ToList();

        await clientBalanceService.RemoveOutdatedOperations(operations, context.CancellationToken);

        return new V1RemoveOutdatedOperationsResponse();
    }

    public override async Task V1QueryOperationsHistory(
        V1QueryOperationsHistoryRequest request,
        IServerStreamWriter<V1QueryOperationsHistoryResponse> responseStream,
        ServerCallContext context)
    {
        var operationsHistory = await clientBalanceService
            .QueryOperationsHistory(
                request.ClientId,
                request.Limit,
                request.Offset,
                context.CancellationToken);

        foreach (var historyEntry in operationsHistory)
        {
            await responseStream.WriteAsync(
                new V1QueryOperationsHistoryResponse
                {
                    OperationId = historyEntry.OperationId.ToString("D"),
                    OperationType = historyEntry.OperationType.ToGrpc(),
                    Amount = historyEntry.Amount.ToMoney(),
                    OperationStatus = historyEntry.OperationStatus.ToGrpc(),
                    OperationTime = historyEntry.OperationTime.ToTimestamp(),
                    TotalCount = historyEntry.TotalCount
                });
        }
    }
}
