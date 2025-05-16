namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;

internal interface IClientsBalanceRepository
{
    Task Insert(
        long clientId,
        decimal balance,
        CancellationToken cancellationToken);

    Task Update(
        long clientId,
        decimal amount,
        CancellationToken cancellationToken);

    Task<decimal> Query(
        long clientId,
        CancellationToken cancellationToken);
}
