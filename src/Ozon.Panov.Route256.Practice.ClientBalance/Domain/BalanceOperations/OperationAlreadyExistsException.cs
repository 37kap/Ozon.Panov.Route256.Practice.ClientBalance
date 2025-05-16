namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

internal sealed class OperationAlreadyExistsException(Guid operationtId) :
    Exception($"Operation with operation_id {operationtId} already exists.");