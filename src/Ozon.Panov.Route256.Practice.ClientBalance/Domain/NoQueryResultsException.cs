using System.Data.Common;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Domain;

internal sealed class NoQueryResultsException(string message = "The query returned no result") :
    DbException(message);