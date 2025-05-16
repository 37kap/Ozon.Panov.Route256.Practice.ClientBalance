using FluentValidation;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.Validation;

public class V1QueryClientBalanceRequestValidator : AbstractValidator<V1QueryClientBalanceRequest>
{
    public V1QueryClientBalanceRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0);
    }
}