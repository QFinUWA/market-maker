namespace MarketMaker.Contracts;

public record ConfigUpdateRequest(
        string? MarketName,
        Dictionary<string, string>? ExchangeNames
);