namespace MarketMaker.Contracts;

public record ConfigUpdateRequest(
    string? ExchangeCode,
    Dictionary<string, string>? MarketNames
);