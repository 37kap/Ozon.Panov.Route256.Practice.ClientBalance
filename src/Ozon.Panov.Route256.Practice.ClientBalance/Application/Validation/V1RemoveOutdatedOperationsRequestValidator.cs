using FluentValidation;
using Ozon.Panov.Route256.Practice.Proto.ClientBalanceGrpc;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Application.Validation;

public class V1RemoveOutdatedOperationsRequestValidator : AbstractValidator<V1RemoveOutdatedOperationsRequest>
{
    public V1RemoveOutdatedOperationsRequestValidator()
    {
        RuleFor(x => x.Operations)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Operations)
            .SetValidator(new OperationValidator());
    }

    private class OperationValidator : AbstractValidator<V1RemoveOutdatedOperationsRequest.Types.Operation>
    {
        public OperationValidator()
        {
            RuleFor(x => x.OperationId)
                .NotEmpty()
                .Must(BeAValidGuid)
                .WithMessage("OperationId must be a valid GUID.");

            RuleFor(x => x.OperationType)
                .IsInEnum()
                .WithMessage("OperationType must be a valid enum value.");
        }

        private static bool BeAValidGuid(string operationId)
        {
            return Guid.TryParse(operationId, out _);
        }
    }
}