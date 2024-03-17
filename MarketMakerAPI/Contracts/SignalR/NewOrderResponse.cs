using System.Collections.Specialized;
using MarketMaker.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketMaker.Contracts;

public record NewOrderResponse(
    string? User,
    string Market,
    long Price,
    int Quantity,
    DateTime TimeStamp,
    Guid Id,
    // List<Guid> ExecutedTradeIds,
    // List<int> ExecutedTradeQuantities
    List<Transaction> Transactions
);