using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.Validation;

public class V1ChangeOperationStatusRequestValidator : AbstractValidator<V1ChangeOperationStatusRequest>
{
    public V1ChangeOperationStatusRequestValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .Must(BeAValidGuid)
            .WithMessage("OperationId must be a valid GUID.");

        RuleFor(x => x.OperationType)
            .IsInEnum()
            .WithMessage("OperationType must be a valid enum value.");

        RuleFor(x => x.ClientId)
            .GreaterThan(0);

        RuleFor(x => x.OperationStatus)
            .IsInEnum()
            .WithMessage("OperationStatus must be a valid enum value.");

        RuleFor(x => x.ChangeTime)
            .NotNull()
            .Must(BeInThePastOrPresent)
            .WithMessage("ChangeTime cannot be in the future.");
    }

    private static bool BeAValidGuid(string operationId)
    {
        return Guid.TryParse(operationId, out _);
    }

    private static bool BeInThePastOrPresent(Timestamp changeTime)
    {
        return changeTime.ToDateTime() <= DateTime.UtcNow;
    }
}