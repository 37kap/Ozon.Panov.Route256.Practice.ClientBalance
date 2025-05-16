namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.BalanceOperations;

internal interface IOperationsRepository
{
    Task Insert(
        OperationEntity topUpOperation,
        CancellationToken cancellationToken);

    Task Update(
        OperationEntity topUpOperation,
        CancellationToken cancellationToken);

    Task<OperationEntity> Query(
        Guid operationId,
        CancellationToken cancellationToken);

    Task Delete(
        IEnumerable<Guid> operationIds,
        CancellationToken cancellationToken);
}