using FluentValidation;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.Validation;

public class V1CreateClientRequestValidator : AbstractValidator<V1CreateClientRequest>
{
    public V1CreateClientRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0);
    }
}