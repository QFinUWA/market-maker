namespace MarketMaker.Models;

public record TransactionEvent(
        DateTime TimeStamp,
        Guid AggressiveOrderId,
        Guid PassiveOrderId,
        int QuantityTraded
    );