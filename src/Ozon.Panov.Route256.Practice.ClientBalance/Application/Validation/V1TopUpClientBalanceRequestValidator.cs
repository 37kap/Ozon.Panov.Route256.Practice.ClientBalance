using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.Validation;

public class V1TopUpClientBalanceRequestValidator : AbstractValidator<V1TopUpClientBalanceRequest>
{
    public V1TopUpClientBalanceRequestValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .Must(BeAValidGuid)
            .WithMessage("OperationId must be a valid GUID.");

        RuleFor(x => x.ClientId)
            .GreaterThan(0);

        RuleFor(x => x.TopUpAmount)
            .NotNull()
            .Must(BePositiveAmount)
            .WithMessage("TopUpAmount must be greater than zero.");

        RuleFor(x => x.OperationTime)
            .NotNull()
            .Must(BeInThePastOrPresent)
            .WithMessage("OperationTime cannot be in the future.");
    }

    private static bool BeAValidGuid(string operationId)
    {
        return Guid.TryParse(operationId, out _);
    }

    private static bool BePositiveAmount(Money amount)
    {
        return amount.Units > 0 || (amount.Units == 0 && amount.Nanos > 0);
    }

    private static bool BeInThePastOrPresent(Timestamp operationTime)
    {
        return operationTime.ToDateTime() <= System.DateTime.UtcNow;
    }
}