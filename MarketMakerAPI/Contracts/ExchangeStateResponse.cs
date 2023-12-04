using MarketMaker.Models;

namespace MarketMaker.Contracts;

public record ExchangeStateResponse(
    List<Order> Orders,
    List<Transaction> Transactions
);

// TODO: add # of users spectating 