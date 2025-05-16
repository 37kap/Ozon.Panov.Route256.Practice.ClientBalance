namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain.ClientBalance;

internal sealed class ClientAlreadyExistsException(long clientId) :
    Exception($"Client with client_id {clientId} already exists.");
