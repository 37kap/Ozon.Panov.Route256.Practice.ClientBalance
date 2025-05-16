using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Presentation.Grpc;

internal sealed class GrpcValidationInterceptor(IServiceProvider serviceProvider) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            foreach (var validator in
                     serviceProvider.GetServices<IValidator<TRequest>>())
            {
                if (await validator
                        .ValidateAsync(request, context.CancellationToken) is { IsValid: false } validationResult)
                {
                    throw new ValidationException(validationResult.Errors);
                }
            }

            return await continuation(request, context);
        }
        catch (ValidationException validationException)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    validationException.Message));
        }
    }
}