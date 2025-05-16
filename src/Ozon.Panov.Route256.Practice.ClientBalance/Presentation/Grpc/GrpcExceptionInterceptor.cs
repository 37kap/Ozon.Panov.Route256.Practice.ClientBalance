using Grpc.Core;
using Grpc.Core.Interceptors;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;
using Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;
using System.ComponentModel.DataAnnotations;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Presentation.Grpc;

internal sealed class GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ClientAlreadyExistsException exception)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, exception.Message));
        }
        catch (OperationAlreadyExistsException exception)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, exception.Message));
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unexpected error occurred.");
            throw new RpcException(new Status(StatusCode.Internal, exception.Message));
        }
    }
}