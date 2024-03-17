namespace MarketMaker.Contracts;

public record NewOrderRequest(
    string Market,
    long Price,
    int Quantity,
    string RequestReference 
);