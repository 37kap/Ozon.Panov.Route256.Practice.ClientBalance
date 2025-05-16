using FluentValidation;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.Validation;

public class V1QueryOperationsHistoryRequestValidator : AbstractValidator<V1QueryOperationsHistoryRequest>
{
    public V1QueryOperationsHistoryRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0);

        RuleFor(x => x.Limit)
            .GreaterThan(0);

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0);
    }
}